using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Application.Users;
using TSTHRMS.Application.Users.Dtos;
using TSTHRMS.Domain.Recruitment;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Auth;
using TSTHRMS.Infrastructure.Persistence;
using TSTHRMS.Infrastructure.Storage;

namespace TSTHRMS.IntegrationTests.Recruitment;

/// <summary>
/// Covers the Slice 1 end-to-end path: raise -> submit -> approve -> publish -> anonymous
/// career-site apply (with dedupe) -> internal pipeline view -> stage move -> talent pool, plus
/// the Manager (Hiring Manager) ownership scoping called out in Section 14.
/// </summary>
public class RecruitmentFlowTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("tsthrms_test")
        .WithUsername("tsthrms")
        .WithPassword("tsthrms_test_password")
        .Build();

    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), $"tsthrms-test-{Guid.NewGuid():N}");

    private Guid _tenantId;
    private Guid _legalEntityId;
    private Guid _productId;
    private Guid _managerUserId;
    private Guid _otherManagerUserId;

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();

        _tenantId = Guid.NewGuid();
        _managerUserId = Guid.NewGuid();
        _otherManagerUserId = Guid.NewGuid();

        await using var context = CreateContext(_tenantId);
        await context.Database.MigrateAsync();

        var legalEntity = new Domain.Tenancy.LegalEntity { TenantId = _tenantId, Name = "Test Entity" };
        var product = new Domain.Tenancy.Product { TenantId = _tenantId, Name = "Test Product" };
        context.LegalEntities.Add(legalEntity);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        _legalEntityId = legalEntity.Id;
        _productId = product.Id;
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task Requisition_to_publish_to_apply_to_pipeline_move_end_to_end()
    {
        await using var context = CreateContext(_tenantId);

        var requisitionService = CreateRequisitionService(context, _managerUserId, []);
        var requisition = await requisitionService.CreateAsync(BuildRequisitionRequest());
        Assert.Equal(RequisitionStatus.Draft, requisition.Status);
        Assert.StartsWith("REQ", requisition.RequisitionCode);

        var submitted = await requisitionService.SubmitForApprovalAsync(requisition.Id);
        Assert.NotNull(submitted);
        Assert.Equal(RequisitionStatus.PendingApproval, submitted!.Status);

        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var approved = await hrService.DecideAsync(
            requisition.Id, RequisitionApprovalDecision.Approved, new RequisitionDecisionRequest("Looks good"));
        Assert.NotNull(approved);
        Assert.Equal(RequisitionStatus.Approved, approved!.Status);
        Assert.Single(approved.Approvals);

        var published = await hrService.PublishAsync(
            requisition.Id, new PublishJobPostingRequest("A great role.", "Mumbai"));
        Assert.NotNull(published);
        Assert.NotNull(published!.JobPosting);
        Assert.True(published.JobPosting!.IsPublished);

        var careerSiteService = CreateCareerSiteService(context);
        var jobs = await careerSiteService.GetPublishedJobsAsync(new PublicJobFilter(null, null, null, null));
        Assert.Single(jobs);

        var applyRequest = new PublicApplicationRequest(
            "Ada", "Lovelace", "ada@example.com", "9999999999", 1000000m, 1300000m, 30, true);
        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());

        var firstApply = await careerSiteService.ApplyAsync(
            published.JobPosting.Slug, applyRequest, CandidateSource.CareerSite, null,
            resume, "resume.pdf", "application/pdf", resume.Length);
        Assert.True(firstApply.Succeeded);
        Assert.NotNull(firstApply.ApplicationId);

        // Re-applying to the same posting with the same email/phone is rejected, not duplicated.
        resume.Position = 0;
        var duplicateApply = await careerSiteService.ApplyAsync(
            published.JobPosting.Slug, applyRequest, CandidateSource.CareerSite, null,
            resume, "resume.pdf", "application/pdf", resume.Length);
        Assert.False(duplicateApply.Succeeded);

        var candidateCount = await context.Candidates.CountAsync();
        Assert.Equal(1, candidateCount);

        var applicantService = CreateApplicantService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var applicants = await applicantService.GetForPostingAsync(published.JobPosting.Id);
        Assert.NotNull(applicants);
        var applicant = Assert.Single(applicants!);
        Assert.Equal("Ada", applicant.FirstName);
        Assert.Equal(ApplicationStage.Applied, applicant.Stage);

        var moved = await applicantService.MoveStageAsync(
            applicant.ApplicationId, new MoveApplicationStageRequest(ApplicationStage.Screening, null));
        Assert.NotNull(moved);
        Assert.Equal(ApplicationStage.Screening, moved!.Stage);

        var talentPoolResult = await applicantService.SetTalentPoolAsync(applicant.CandidateId, true);
        Assert.True(talentPoolResult);

        var refreshedApplicants = await applicantService.GetForPostingAsync(published.JobPosting.Id);
        Assert.True(refreshedApplicants!.Single().IsInTalentPool);
    }

    [Fact]
    public async Task Applying_to_a_second_posting_surfaces_as_an_other_application_and_in_the_talent_pool()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);
        var applicantService = CreateApplicantService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);

        var firstPosting = await PublishRequisitionAsync(managerService, hrService, "Senior Engineer");
        var secondPosting = await PublishRequisitionAsync(managerService, hrService, "Staff Engineer");

        var applyRequest = new PublicApplicationRequest(
            "Ada", "Lovelace", "ada2@example.com", "9999999998", null, null, null, true);

        using (var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray()))
        {
            var firstApply = await careerSiteService.ApplyAsync(
                firstPosting.Slug, applyRequest, CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
            Assert.True(firstApply.Succeeded);
        }

        using (var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray()))
        {
            var secondApply = await careerSiteService.ApplyAsync(
                secondPosting.Slug, applyRequest, CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
            Assert.True(secondApply.Succeeded);
        }

        var applicantsOnSecondPosting = await applicantService.GetForPostingAsync(secondPosting.Id);
        var applicant = Assert.Single(applicantsOnSecondPosting!);
        var otherApplication = Assert.Single(applicant.OtherApplications);
        Assert.Equal(firstPosting.Id, otherApplication.JobPostingId);

        await applicantService.SetTalentPoolAsync(applicant.CandidateId, true);
        var talentPool = await applicantService.GetTalentPoolAsync();
        var talentPoolEntry = Assert.Single(talentPool);
        Assert.Equal("Staff Engineer", talentPoolEntry.MostRecentJobPostingTitle);
    }

    private async Task<JobPostingDto> PublishRequisitionAsync(
        IJobRequisitionService managerService, IJobRequisitionService hrService, string title)
    {
        var requisition = await managerService.CreateAsync(BuildRequisitionRequest() with { Title = title });
        await managerService.SubmitForApprovalAsync(requisition.Id);
        await hrService.DecideAsync(requisition.Id, RequisitionApprovalDecision.Approved, new RequisitionDecisionRequest(null));
        var published = await hrService.PublishAsync(requisition.Id, new PublishJobPostingRequest("Great role.", "Remote"));
        return published!.JobPosting!;
    }

    [Fact]
    public async Task A_manager_only_sees_and_can_only_act_on_their_own_requisitions()
    {
        await using var context = CreateContext(_tenantId);

        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var requisition = await managerService.CreateAsync(BuildRequisitionRequest());

        var otherManagerService = CreateRequisitionService(context, _otherManagerUserId, []);
        var blockedRead = await otherManagerService.GetByIdAsync(requisition.Id);
        Assert.Null(blockedRead);

        var blockedSubmit = await otherManagerService.SubmitForApprovalAsync(requisition.Id);
        Assert.Null(blockedSubmit);

        var otherManagerList = await otherManagerService.GetListAsync(null);
        Assert.DoesNotContain(otherManagerList, r => r.Id == requisition.Id);

        var ownerList = await managerService.GetListAsync(null);
        Assert.Contains(ownerList, r => r.Id == requisition.Id);
    }

    [Fact]
    public async Task Scorecard_visibility_is_gated_until_every_panelist_submits_except_for_HR_and_the_authors_own()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);

        var posting = await PublishRequisitionAsync(managerService, hrService, "Backend Engineer");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyRequest = new PublicApplicationRequest(
            "Grace", "Hopper", "grace@example.com", "9999999997", null, null, null, true);
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, applyRequest, CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var applicationId = applyResult.ApplicationId!.Value;

        var panelistA = Guid.NewGuid();
        var panelistB = Guid.NewGuid();
        var outsider = Guid.NewGuid();

        var hrInterviewService = CreateInterviewService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var scheduled = await hrInterviewService.ScheduleAsync(
            applicationId,
            new ScheduleInterviewRequest(
                ApplicationStage.InterviewRound1, DateTimeOffset.UtcNow.AddDays(1), 45, "https://meet.example.com/x",
                [panelistA, panelistB]),
            default);
        Assert.NotNull(scheduled);

        var panelistAService = CreateInterviewService(context, panelistA, []);
        var panelistBService = CreateInterviewService(context, panelistB, []);
        var outsiderService = CreateInterviewService(context, outsider, []);
        var managerInterviewService = CreateInterviewService(context, _managerUserId, []);

        // An unassigned user can't submit.
        var blockedSubmit = await outsiderService.SubmitScorecardAsync(
            scheduled!.Id, new SubmitScorecardRequest(4, 4, 4, 4, InterviewRecommendation.Yes, null));
        Assert.Null(blockedSubmit);

        await panelistAService.SubmitScorecardAsync(
            scheduled.Id, new SubmitScorecardRequest(5, 4, 5, 4, InterviewRecommendation.StrongYes, "Great"));

        // A repeat submission from the same panelist is rejected (append-only, no edits).
        var duplicateSubmit = await panelistAService.SubmitScorecardAsync(
            scheduled.Id, new SubmitScorecardRequest(1, 1, 1, 1, InterviewRecommendation.StrongNo, "Changed my mind"));
        Assert.Null(duplicateSubmit);

        var managerViewBeforeAllSubmitted = await managerInterviewService.GetForApplicationAsync(applicationId);
        Assert.Empty(managerViewBeforeAllSubmitted!.Single().VisibleScorecards);

        var hrViewBeforeAllSubmitted = await hrInterviewService.GetForApplicationAsync(applicationId);
        Assert.Single(hrViewBeforeAllSubmitted!.Single().VisibleScorecards);

        var panelistAOwnView = await panelistAService.GetForApplicationAsync(applicationId);
        Assert.Single(panelistAOwnView!.Single().VisibleScorecards);

        await panelistBService.SubmitScorecardAsync(
            scheduled.Id, new SubmitScorecardRequest(3, 3, 3, 3, InterviewRecommendation.Yes, null));

        var managerViewAfterAllSubmitted = await managerInterviewService.GetForApplicationAsync(applicationId);
        Assert.Equal(2, managerViewAfterAllSubmitted!.Single().VisibleScorecards.Count);
        Assert.True(managerViewAfterAllSubmitted!.Single().AllScorecardsSubmitted);

        var myInterviews = await panelistAService.GetMyInterviewsAsync();
        Assert.True(Assert.Single(myInterviews).HasSubmitted);
    }

    [Fact]
    public async Task Assessment_send_submit_and_score_flow_including_below_threshold_retake_cooldown()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);
        var hrAssessmentService = CreateAssessmentService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);

        var posting = await PublishRequisitionAsync(managerService, hrService, "Data Analyst");

        // No test configured yet - sending should fail.
        using var firstResume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyRequest = new PublicApplicationRequest(
            "Marie", "Curie", "marie@example.com", "9999999996", null, null, null, true);
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, applyRequest, CandidateSource.CareerSite, null, firstResume, "resume.pdf", "application/pdf", firstResume.Length);
        var applicationId = applyResult.ApplicationId!.Value;

        var blockedSend = await hrAssessmentService.SendAssessmentAsync(applicationId);
        Assert.False(blockedSend.Succeeded);

        var config = await hrAssessmentService.ConfigureTestAsync(
            posting.Id, new TestConfigurationRequest(true, AssessmentType.AptitudeTest, "Answer honestly.", 45, 5, 60, 6));
        Assert.NotNull(config);
        Assert.True(config!.IsEnabled);

        var sendResult = await hrAssessmentService.SendAssessmentAsync(applicationId);
        Assert.True(sendResult.Succeeded);
        Assert.NotNull(sendResult.Assessment);

        // Sending a second time for the same application is rejected.
        var duplicateSend = await hrAssessmentService.SendAssessmentAsync(applicationId);
        Assert.False(duplicateSend.Succeeded);

        var token = await context.AssessmentSubmissions
            .Where(a => a.ApplicationId == applicationId)
            .Select(a => a.Token)
            .SingleAsync();

        var publicAssessment = await hrAssessmentService.GetPublicAssessmentAsync(token);
        Assert.NotNull(publicAssessment);
        Assert.False(publicAssessment!.IsExpired);
        Assert.False(publicAssessment.AlreadySubmitted);
        Assert.Equal("Data Analyst", publicAssessment.JobTitle);

        var submitted = await hrAssessmentService.SubmitPublicAssessmentAsync(
            token, new PublicAssessmentSubmissionRequest("My detailed answer."));
        Assert.True(submitted);

        // A second submission attempt is rejected - the candidate only gets one shot.
        var duplicateSubmit = await hrAssessmentService.SubmitPublicAssessmentAsync(
            token, new PublicAssessmentSubmissionRequest("Changed my answer."));
        Assert.False(duplicateSubmit);

        var scored = await hrAssessmentService.ScoreAsync(
            sendResult.Assessment!.Id, new ScoreAssessmentRequest(40, "Missed the core question."));
        Assert.NotNull(scored);
        Assert.False(scored!.Passed);
        Assert.NotNull(scored.RetakeAllowedAfter);
        Assert.True(scored.RetakeAllowedAfter >= DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(5));

        var applicants = await CreateApplicantService(context, Guid.NewGuid(), [RoleNames.HRAdmin])
            .GetForPostingAsync(posting.Id);
        var applicantAssessment = Assert.Single(applicants!).Assessment;
        Assert.NotNull(applicantAssessment);
        Assert.Equal(40, applicantAssessment!.Score);
        Assert.False(applicantAssessment.Passed);
    }

    [Fact]
    public async Task Offer_revision_resets_approval_and_accept_moves_the_application_to_offer_accepted()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);
        var hrOfferService = CreateOfferService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);

        var posting = await PublishRequisitionAsync(managerService, hrService, "Product Manager");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyRequest = new PublicApplicationRequest(
            "Alan", "Turing", "alan@example.com", "9999999995", null, null, null, true);
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, applyRequest, CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var applicationId = applyResult.ApplicationId!.Value;

        var created = await hrOfferService.CreateAsync(
            applicationId,
            new CreateOrReviseOfferRequest("Product Manager", new DateOnly(2026, 9, 1), 2000000m, 1600000m, 400000m, null, null, null));
        Assert.NotNull(created);
        Assert.Equal(OfferStatus.Draft, created!.Status);
        Assert.Single(created.Versions);

        // A second offer for the same application is rejected - revise the existing one instead.
        var duplicateCreate = await hrOfferService.CreateAsync(
            applicationId, new CreateOrReviseOfferRequest(null, new DateOnly(2026, 9, 1), 100m, null, null, null, null, null));
        Assert.Null(duplicateCreate);

        var submitted = await hrOfferService.SubmitForApprovalAsync(created.Id);
        Assert.Equal(OfferStatus.PendingApproval, submitted!.Status);

        var approved = await hrOfferService.ApproveAsync(created.Id, new OfferDecisionRequest("Looks good"));
        Assert.Equal(OfferStatus.Approved, approved!.Status);

        // Revising an approved offer resets it to Draft - a changed CTC needs re-approval.
        var revised = await hrOfferService.ReviseAsync(
            created.Id,
            new CreateOrReviseOfferRequest("Senior Product Manager", new DateOnly(2026, 9, 1), 2200000m, 1700000m, 500000m, null, null, "Candidate negotiated"));
        Assert.Equal(OfferStatus.Draft, revised!.Status);
        Assert.Equal(2, revised.Versions.Count);

        await hrOfferService.SubmitForApprovalAsync(created.Id);
        await hrOfferService.ApproveAsync(created.Id, new OfferDecisionRequest(null));
        var sent = await hrOfferService.SendAsync(created.Id, new SendOfferRequest(7));
        Assert.Equal(OfferStatus.Sent, sent!.Status);
        Assert.NotNull(sent.ExpiresAt);

        var offerToken = await context.Offers.Where(o => o.ApplicationId == applicationId).Select(o => o.Token).SingleAsync();

        var publicOffer = await hrOfferService.GetPublicOfferAsync(offerToken);
        Assert.NotNull(publicOffer);
        Assert.False(publicOffer!.IsExpired);
        Assert.Equal(2200000m, publicOffer.AnnualCtc);
        Assert.Equal("Senior Product Manager", publicOffer.Designation);

        var accepted = await hrOfferService.RespondPublicOfferAsync(offerToken, new PublicOfferDecisionRequest(true, null));
        Assert.True(accepted);

        var application = await context.Applications.AsNoTracking().SingleAsync(a => a.Id == applicationId);
        Assert.Equal(ApplicationStage.OfferAccepted, application.Stage);

        // Responding again is rejected - the offer is no longer in the Sent state.
        var secondResponse = await hrOfferService.RespondPublicOfferAsync(offerToken, new PublicOfferDecisionRequest(false, "Changed my mind"));
        Assert.False(secondResponse);
    }

    [Fact]
    public async Task Declining_an_offer_rejects_the_application_with_the_decline_reason()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);
        var hrOfferService = CreateOfferService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);

        var posting = await PublishRequisitionAsync(managerService, hrService, "QA Engineer");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyRequest = new PublicApplicationRequest(
            "Katherine", "Johnson", "katherine@example.com", "9999999994", null, null, null, true);
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, applyRequest, CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var applicationId = applyResult.ApplicationId!.Value;

        var offer = await hrOfferService.CreateAsync(
            applicationId, new CreateOrReviseOfferRequest("QA Engineer", new DateOnly(2026, 9, 15), 1200000m, null, null, null, null, null));
        await hrOfferService.SubmitForApprovalAsync(offer!.Id);
        await hrOfferService.ApproveAsync(offer.Id, new OfferDecisionRequest(null));
        await hrOfferService.SendAsync(offer.Id, new SendOfferRequest(7));

        var token = await context.Offers.Where(o => o.ApplicationId == applicationId).Select(o => o.Token).SingleAsync();

        var declined = await hrOfferService.RespondPublicOfferAsync(
            token, new PublicOfferDecisionRequest(false, "Accepted a counter-offer"));
        Assert.True(declined);

        var application = await context.Applications.AsNoTracking().SingleAsync(a => a.Id == applicationId);
        Assert.Equal(ApplicationStage.Rejected, application.Stage);
        Assert.Contains("Accepted a counter-offer", application.RejectionReason);

        var finalOffer = await hrOfferService.GetForApplicationAsync(applicationId);
        Assert.Equal(OfferStatus.Declined, finalOffer!.Status);
    }

    [Fact]
    public async Task Candidate_can_request_and_verify_an_otp_and_only_sees_their_own_applications()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);

        var posting = await PublishRequisitionAsync(managerService, hrService, "Site Reliability Engineer");

        using (var resumeA = new MemoryStream("%PDF-1.4 resume"u8.ToArray()))
        {
            var applyA = await careerSiteService.ApplyAsync(
                posting.Slug, new PublicApplicationRequest("Ada", "Byron", "ada.byron@example.com", "9999999993", null, null, null, true),
                CandidateSource.CareerSite, null, resumeA, "resume.pdf", "application/pdf", resumeA.Length);
            Assert.True(applyA.Succeeded);
        }

        using (var resumeB = new MemoryStream("%PDF-1.4 resume"u8.ToArray()))
        {
            var applyB = await careerSiteService.ApplyAsync(
                posting.Slug, new PublicApplicationRequest("Bob", "Noyce", "bob.noyce@example.com", "9999999992", null, null, null, true),
                CandidateSource.CareerSite, null, resumeB, "resume.pdf", "application/pdf", resumeB.Length);
            Assert.True(applyB.Succeeded);
        }

        var emailSender = new CapturingEmailSender();
        var authService = CreateCandidatePortalAuthService(context, emailSender);

        await authService.RequestOtpAsync("ada.byron@example.com");
        Assert.NotNull(emailSender.LastHtmlBody);
        var code = Regex.Match(emailSender.LastHtmlBody!, @"\d{6}").Value;
        Assert.Equal(6, code.Length);

        var wrongCode = code == "000000" ? "111111" : "000000";
        var wrongCodeResult = await authService.VerifyOtpAsync("ada.byron@example.com", wrongCode);
        Assert.False(wrongCodeResult.Succeeded);

        var loginResult = await authService.VerifyOtpAsync("ada.byron@example.com", code);
        Assert.True(loginResult.Succeeded);
        Assert.NotNull(loginResult.AccessToken);
        Assert.Equal("Ada Byron", loginResult.CandidateName);

        // The code is single-use - a repeat attempt with the same code is rejected.
        var reuseResult = await authService.VerifyOtpAsync("ada.byron@example.com", code);
        Assert.False(reuseResult.Succeeded);

        var adaCandidateId = await context.Candidates.AsNoTracking()
            .Where(c => c.Email == "ada.byron@example.com").Select(c => c.Id).SingleAsync();
        var bobCandidateId = await context.Candidates.AsNoTracking()
            .Where(c => c.Email == "bob.noyce@example.com").Select(c => c.Id).SingleAsync();

        var adaApplications = await new CandidatePortalService(context, new TestCandidateContext(adaCandidateId))
            .GetMyApplicationsAsync();
        var bobApplications = await new CandidatePortalService(context, new TestCandidateContext(bobCandidateId))
            .GetMyApplicationsAsync();

        var adaApplication = Assert.Single(adaApplications);
        var bobApplication = Assert.Single(bobApplications);
        Assert.Equal(posting.Title, adaApplication.JobPostingTitle);
        Assert.NotEqual(adaApplication.ApplicationId, bobApplication.ApplicationId);

        var unauthenticated = await new CandidatePortalService(context, new TestCandidateContext(null)).GetMyApplicationsAsync();
        Assert.Empty(unauthenticated);
    }

    [Fact]
    public async Task Employee_referral_tags_the_candidate_and_status_is_only_visible_to_the_referrer()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);

        var posting = await PublishRequisitionAsync(managerService, hrService, "Sales Executive");

        var referringEmployeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();

        var referralService = new ReferralService(
            context, new TestCurrentUserService(Guid.NewGuid(), [], referringEmployeeId), careerSiteService);

        // No resume attached - Section 4 referrals don't require one, unlike a direct application.
        var referralResult = await referralService.SubmitReferralAsync(
            posting.Slug, new ReferralSubmissionRequest("Grace", "Murray", "grace.murray@example.com", "9999999991"),
            null, null, null, 0);
        Assert.True(referralResult.Succeeded);

        var candidate = await context.Candidates.AsNoTracking().SingleAsync(c => c.Email == "grace.murray@example.com");
        Assert.Equal(CandidateSource.Referral, candidate.Source);
        Assert.Equal(referringEmployeeId, candidate.ReferredByEmployeeId);

        var myReferrals = await referralService.GetMyReferralsAsync();
        var referral = Assert.Single(myReferrals);
        Assert.Equal("Grace Murray", referral.CandidateName);
        Assert.Equal(posting.Title, referral.JobPostingTitle);

        var otherReferralService = new ReferralService(
            context, new TestCurrentUserService(Guid.NewGuid(), [], otherEmployeeId), careerSiteService);
        Assert.Empty(await otherReferralService.GetMyReferralsAsync());

        // A login with no linked employee record is blocked outright, rather than silently
        // submitting an unattributed referral.
        var noEmployeeReferralService = new ReferralService(
            context, new TestCurrentUserService(Guid.NewGuid(), []), careerSiteService);
        var blockedResult = await noEmployeeReferralService.SubmitReferralAsync(
            posting.Slug, new ReferralSubmissionRequest("X", "Y", "x.y@example.com", "9999999990"), null, null, null, 0);
        Assert.False(blockedResult.Succeeded);
    }

    private CandidatePortalAuthService CreateCandidatePortalAuthService(ApplicationDbContext context, IEmailSender emailSender) =>
        new(context, new TestTenantContext(_tenantId), emailSender, new JwtTokenGenerator(new TestJwtOptions()),
            NullLogger<CandidatePortalAuthService>.Instance);

    private OfferService CreateOfferService(ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles) =>
        new(context, new TestTenantContext(_tenantId), new TestCurrentUserService(userId, roles),
            new NoOpFrontendLinkBuilder(), new NoOpEmailSender(), NullLogger<OfferService>.Instance);

    private AssessmentService CreateAssessmentService(ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles) =>
        new(context, new TestTenantContext(_tenantId), new TestCurrentUserService(userId, roles),
            new NoOpFrontendLinkBuilder(), new NoOpEmailSender(), NullLogger<AssessmentService>.Instance);

    private InterviewService CreateInterviewService(ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles) =>
        new(context, new TestCurrentUserService(userId, roles), new NoOpUserDirectory(), new NoOpUserManagementService(),
            new NoOpEmailSender(), NullLogger<InterviewService>.Instance);

    private JobRequisitionWriteRequest BuildRequisitionRequest() => new(
        "Senior Engineer", _legalEntityId, _productId, "L4", "Engineering",
        Domain.Employees.EmploymentType.FullTime, 2, 1500000m,
        RequisitionReason.NewRole, "Team is growing", 2);

    private JobRequisitionService CreateRequisitionService(
        ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles) =>
        new(context, new SequenceGenerator(context, new TestTenantContext(_tenantId)),
            new TestCurrentUserService(userId, roles));

    private ApplicantService CreateApplicantService(
        ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles) =>
        new(context, new TestCurrentUserService(userId, roles));

    private CareerSiteService CreateCareerSiteService(ApplicationDbContext context) =>
        new(context, new TestTenantContext(_tenantId), new LocalFileStorageService(new TestFileStorageOptions(_storageRoot)),
            new NoOpEmailSender(), NullLogger<CareerSiteService>.Instance);

    private ApplicationDbContext CreateContext(Guid tenantId)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(_mysql.GetConnectionString(), new MySqlServerVersion(new Version(8, 4, 0)));
        return new ApplicationDbContext(optionsBuilder.Options, new TestTenantContext(tenantId), new TestCurrentUserService(null, []));
    }

    private class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public bool IsResolved => tenantId != Guid.Empty;
    }

    private class TestCurrentUserService(Guid? userId, IReadOnlyCollection<string> roles, Guid? employeeId = null) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public IReadOnlyCollection<string> Roles => roles;
        public Guid? EmployeeId => employeeId;
    }

    private class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>Captures the last sent email so a test can pull the OTP code out of it - there's
    /// no other way to learn the plaintext code, since only its hash is ever persisted.</summary>
    private class CapturingEmailSender : IEmailSender
    {
        public string? LastHtmlBody { get; private set; }

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            LastHtmlBody = htmlBody;
            return Task.CompletedTask;
        }
    }

    private class TestCandidateContext(Guid? candidateId) : ICandidateContext
    {
        public Guid? CandidateId => candidateId;
    }

    private class TestJwtOptions : Microsoft.Extensions.Options.IOptions<JwtSettings>
    {
        public JwtSettings Value { get; } = new()
        {
            Key = "test-only-signing-key-at-least-32-characters-long",
            Issuer = "TSTHRMS.Test",
            Audience = "TSTHRMS.Test.Client",
        };
    }

    /// <summary>Display-name lookups aren't asserted on in these tests - only that the right
    /// scorecard rows are (in)visible - so a fixed placeholder is enough.</summary>
    private class NoOpUserDirectory : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.ToDictionary(id => id, _ => "Test User"));
    }

    /// <summary>Only GetInterviewerCandidatesAsync (not exercised by these tests) calls into
    /// this - the other members exist solely to satisfy the interface.</summary>
    private class NoOpUserManagementService : IUserManagementService
    {
        public Task<IReadOnlyList<UserSummaryDto>> GetListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserSummaryDto>>([]);

        public Task<UserCreationResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private class TestFileStorageOptions(string rootPath) : Microsoft.Extensions.Options.IOptions<LocalFileStorageOptions>
    {
        public LocalFileStorageOptions Value { get; } = new() { RootPath = rootPath };
    }

    private class NoOpFrontendLinkBuilder : IFrontendLinkBuilder
    {
        public string BuildCareerSiteAssessmentLink(string tenantSlug, string token) =>
            $"https://example.test/careers/{tenantSlug}/assessment/{token}";

        public string BuildCareerSiteOfferLink(string tenantSlug, string token) =>
            $"https://example.test/careers/{tenantSlug}/offer/{token}";
    }
}
