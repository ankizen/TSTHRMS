using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Application.Users;
using TSTHRMS.Application.Users.Dtos;
using TSTHRMS.Domain.Employees;
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

    [Fact]
    public async Task Accepting_an_offer_auto_creates_a_preboarding_checklist_and_candidate_can_submit_tasks()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);

        var posting = await PublishRequisitionAsync(managerService, hrService, "DevOps Engineer");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, new PublicApplicationRequest("Margaret", "Hamilton", "margaret@example.com", "9999999989", null, null, null, true),
            CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var applicationId = applyResult.ApplicationId!.Value;

        var welcomeEmailSender = new CapturingEmailSender();
        var hrOfferService = CreateOfferService(context, Guid.NewGuid(), [RoleNames.HRAdmin], welcomeEmailSender);

        var offer = await hrOfferService.CreateAsync(
            applicationId, new CreateOrReviseOfferRequest("DevOps Engineer", new DateOnly(2026, 9, 1), 1800000m, null, null, null, null, null));
        await hrOfferService.SubmitForApprovalAsync(offer!.Id);
        await hrOfferService.ApproveAsync(offer.Id, new OfferDecisionRequest(null));
        await hrOfferService.SendAsync(offer.Id, new SendOfferRequest(7));

        var token = await context.Offers.Where(o => o.ApplicationId == applicationId).Select(o => o.Token).SingleAsync();
        await hrOfferService.RespondPublicOfferAsync(token, new PublicOfferDecisionRequest(true, null));

        // The welcome email fired automatically as part of checklist creation.
        Assert.NotNull(welcomeEmailSender.LastHtmlBody);

        var hrPreboardingService = CreatePreboardingService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var hrChecklist = await hrPreboardingService.GetChecklistAsync(applicationId);
        Assert.Equal(6, hrChecklist!.Count);
        var welcomeTask = hrChecklist.Single(t => t.TaskType == PreboardingTaskType.WelcomeCommunication);
        Assert.Equal(PreboardingTaskStatus.Completed, welcomeTask.Status);

        var candidateId = await context.Candidates.AsNoTracking()
            .Where(c => c.Email == "margaret@example.com").Select(c => c.Id).SingleAsync();
        var candidatePreboardingService = CreatePreboardingService(context, Guid.NewGuid(), [], candidateId);

        using (var certificate = new MemoryStream("%PDF-1.4 cert"u8.ToArray()))
        {
            var submitted = await candidatePreboardingService.SubmitDocumentTaskAsync(
                applicationId, PreboardingTaskType.EducationCertificate, certificate, "degree.pdf", "application/pdf", certificate.Length);
            Assert.True(submitted);
        }

        var bankDetailsSubmitted = await candidatePreboardingService.SubmitBankDetailsAsync(
            applicationId, new SubmitBankDetailsRequest("1234567890123456", "HDFC0001234"));
        Assert.True(bankDetailsSubmitted);

        var updatedChecklist = await hrPreboardingService.GetChecklistAsync(applicationId);
        var educationTask = updatedChecklist!.Single(t => t.TaskType == PreboardingTaskType.EducationCertificate);
        Assert.Equal(PreboardingTaskStatus.Completed, educationTask.Status);
        Assert.NotNull(educationTask.DocumentId);

        var bankTask = updatedChecklist!.Single(t => t.TaskType == PreboardingTaskType.BankDetails);
        Assert.EndsWith("3456", bankTask.BankAccountNumberMasked);
        Assert.DoesNotContain("1234567890", bankTask.BankAccountNumberMasked ?? "");

        // A different candidate can't touch this application's checklist.
        var otherCandidateService = CreatePreboardingService(context, Guid.NewGuid(), [], Guid.NewGuid());
        var blockedBankSubmit = await otherCandidateService.SubmitBankDetailsAsync(
            applicationId, new SubmitBankDetailsRequest("999", "ICIC0009999"));
        Assert.False(blockedBankSubmit);
    }

    [Fact]
    public async Task Bgv_can_be_initiated_and_its_status_updated_within_the_same_ownership_scoping()
    {
        await using var context = CreateContext(_tenantId);
        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);

        var posting = await PublishRequisitionAsync(managerService, hrService, "Finance Analyst");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, new PublicApplicationRequest("Rear", "Admiral", "rear.admiral@example.com", "9999999988", null, null, null, true),
            CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var applicationId = applyResult.ApplicationId!.Value;

        var hrBgvService = CreateBgvService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);

        var notStarted = await hrBgvService.GetForApplicationAsync(applicationId);
        Assert.Equal(BgvStatus.NotStarted, notStarted!.Status);

        var initiated = await hrBgvService.InitiateAsync(
            applicationId, new InitiateBgvRequest("AUTHBRIDGE-12345", true));
        Assert.Equal(BgvStatus.Initiated, initiated!.Status);
        Assert.True(initiated.IsConditionalJoining);

        var flagged = await hrBgvService.UpdateStatusAsync(
            applicationId, new UpdateBgvStatusRequest(BgvStatus.DiscrepancyFound, "Mismatched employment dates"));
        Assert.Equal(BgvStatus.DiscrepancyFound, flagged!.Status);
        Assert.Equal("Mismatched employment dates", flagged.DiscrepancyNotes);

        // An unrelated Manager can't see or touch this application's BGV record.
        var otherManagerBgvService = CreateBgvService(context, _otherManagerUserId, []);
        Assert.Null(await otherManagerBgvService.GetForApplicationAsync(applicationId));
        Assert.Null(await otherManagerBgvService.UpdateStatusAsync(
            applicationId, new UpdateBgvStatusRequest(BgvStatus.Clear, null)));
    }

    [Fact]
    public async Task Converting_an_accepted_offer_creates_an_employee_and_an_onboarding_checklist()
    {
        await using var context = CreateContext(_tenantId);
        var hiringManagerUserId = _managerUserId;
        var reportingManagerEmployeeId = Guid.NewGuid();
        var userDirectory = new NoOpUserDirectory(new Dictionary<Guid, Guid> { [hiringManagerUserId] = reportingManagerEmployeeId });

        var managerService = CreateRequisitionService(context, hiringManagerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);

        var posting = await PublishRequisitionAsync(managerService, hrService, "Backend Developer");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, new PublicApplicationRequest("Grace", "Hopper", "grace.hopper@example.com", "9999999987", null, null, null, true),
            CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var applicationId = applyResult.ApplicationId!.Value;

        var hrOfferService = CreateOfferService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var offer = await hrOfferService.CreateAsync(
            applicationId, new CreateOrReviseOfferRequest("Backend Developer", new DateOnly(2026, 9, 1), 2400000m, null, null, null, null, null));
        await hrOfferService.SubmitForApprovalAsync(offer!.Id);
        await hrOfferService.ApproveAsync(offer.Id, new OfferDecisionRequest(null));
        await hrOfferService.SendAsync(offer.Id, new SendOfferRequest(7));
        var offerToken = await context.Offers.Where(o => o.ApplicationId == applicationId).Select(o => o.Token).SingleAsync();
        await hrOfferService.RespondPublicOfferAsync(offerToken, new PublicOfferDecisionRequest(true, null));

        // Pre-boarding data that should carry across into the new Employee record.
        var candidateId = await context.Candidates.AsNoTracking()
            .Where(c => c.Email == "grace.hopper@example.com").Select(c => c.Id).SingleAsync();
        var candidatePreboardingService = CreatePreboardingService(context, Guid.NewGuid(), [], candidateId);
        await candidatePreboardingService.SubmitBankDetailsAsync(
            applicationId, new SubmitBankDetailsRequest("1112223334445556", "ICIC0001112"));
        using (var certificate = new MemoryStream("%PDF-1.4 cert"u8.ToArray()))
        {
            await candidatePreboardingService.SubmitDocumentTaskAsync(
                applicationId, PreboardingTaskType.EducationCertificate, certificate, "degree.pdf", "application/pdf", certificate.Length);
        }

        var hrOnboardingService = CreateOnboardingService(context, Guid.NewGuid(), [RoleNames.HRAdmin], userDirectory: userDirectory);
        var conversion = await hrOnboardingService.ConvertToEmployeeAsync(applicationId);

        Assert.True(conversion.Succeeded);
        Assert.NotNull(conversion.Employee);
        Assert.StartsWith("EMP", conversion.Employee!.EmployeeCode);
        Assert.Equal("Grace", conversion.Employee.FirstName);
        Assert.Equal(200000m, conversion.Employee.MonthlyGrossSalary); // 2,400,000 / 12
        Assert.Equal(Gender.PreferNotToSay, conversion.Employee.Gender);
        Assert.EndsWith("5556", conversion.Employee.BankAccountNumberMasked);

        var employee = await context.Employees.AsNoTracking().SingleAsync(e => e.Id == conversion.Employee.Id);
        Assert.Equal(applicationId, employee.SourceApplicationId);
        Assert.Equal(reportingManagerEmployeeId, employee.ReportingManagerId);

        var application = await context.Applications.AsNoTracking().SingleAsync(a => a.Id == applicationId);
        Assert.Equal(ApplicationStage.Hired, application.Stage);

        // Converting an already-Hired application again fails outright.
        var secondConversion = await hrOnboardingService.ConvertToEmployeeAsync(applicationId);
        Assert.False(secondConversion.Succeeded);

        var employeeDocument = await context.EmployeeDocuments.AsNoTracking()
            .SingleAsync(d => d.EmployeeId == employee.Id);
        Assert.Equal(Domain.Documents.EmployeeDocumentCategory.EducationCertificate, employeeDocument.Category);

        var checklist = await hrOnboardingService.GetChecklistAsync(employee.Id);
        Assert.Equal(5, checklist!.Count);
        Assert.All(checklist, item => Assert.Equal(new DateOnly(2026, 9, 1), item.DueDate));

        // The reporting manager can see/act on the checklist; an unrelated manager can't.
        var reportingManagerOnboardingService = CreateOnboardingService(
            context, Guid.NewGuid(), [], reportingManagerEmployeeId, userDirectory);
        Assert.NotNull(await reportingManagerOnboardingService.GetChecklistAsync(employee.Id));

        var unrelatedManagerOnboardingService = CreateOnboardingService(
            context, Guid.NewGuid(), [], Guid.NewGuid(), userDirectory);
        Assert.Null(await unrelatedManagerOnboardingService.GetChecklistAsync(employee.Id));

        // Completing the policy-acknowledgement task also stamps Employee.PoshAcknowledgedAt.
        var policyItem = checklist.Single(i => i.TaskType == OnboardingTaskType.PolicyAcknowledgement);
        var completed = await hrOnboardingService.CompleteItemAsync(policyItem.Id);
        Assert.Equal(OnboardingTaskStatus.Completed, completed!.Status);

        var employeeAfterAcknowledgement = await context.Employees.AsNoTracking().SingleAsync(e => e.Id == employee.Id);
        Assert.NotNull(employeeAfterAcknowledgement.PoshAcknowledgedAt);
    }

    [Fact]
    public async Task Recruitment_report_aggregates_correctly_and_scopes_by_requisition_ownership()
    {
        await using var context = CreateContext(_tenantId);
        var now = DateTimeOffset.UtcNow;

        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);

        // Requisition A (raised by _managerUserId) - backdated 45 days so it shows as ageing/stale.
        var managerAService = CreateRequisitionService(context, _managerUserId, []);
        var postingA = await PublishRequisitionAsync(managerAService, hrService, "Report Test Role A");

        // Requisition B (raised by _otherManagerUserId) - backdated 10 days, not stale.
        var managerBService = CreateRequisitionService(context, _otherManagerUserId, []);
        var postingB = await PublishRequisitionAsync(managerBService, hrService, "Report Test Role B");

        var requisitionA = await context.JobRequisitions.SingleAsync(r => r.JobPosting!.Id == postingA.Id);
        requisitionA.CreatedAt = now.AddDays(-45);
        var requisitionB = await context.JobRequisitions.SingleAsync(r => r.JobPosting!.Id == postingB.Id);
        requisitionB.CreatedAt = now.AddDays(-10);
        await context.SaveChangesAsync();

        // A1: CareerSite, under posting A, applied 20 days ago, hired 5 days ago (15-day time-to-hire), offer Accepted.
        var candidate1 = new Candidate
        {
            FirstName = "A1", LastName = "Candidate", Email = "a1@example.com", Phone = "9000000001",
            Source = CandidateSource.CareerSite, ConsentGivenAt = now,
        };
        // A2: Referral, under posting A, applied 10 days ago, still Screening (active, not hired).
        var candidate2 = new Candidate
        {
            FirstName = "A2", LastName = "Candidate", Email = "a2@example.com", Phone = "9000000002",
            Source = CandidateSource.Referral, ConsentGivenAt = now,
        };
        // A3: CareerSite, under posting B, applied 40 days ago, Rejected, offer Declined.
        var candidate3 = new Candidate
        {
            FirstName = "A3", LastName = "Candidate", Email = "a3@example.com", Phone = "9000000003",
            Source = CandidateSource.CareerSite, ConsentGivenAt = now,
        };
        context.Candidates.AddRange(candidate1, candidate2, candidate3);
        await context.SaveChangesAsync();

        var application1 = new JobApplication
        {
            CandidateId = candidate1.Id, JobPostingId = postingA.Id, Stage = ApplicationStage.Hired,
            StageChangedAt = now.AddDays(-5), AppliedAt = now.AddDays(-20),
        };
        var application2 = new JobApplication
        {
            CandidateId = candidate2.Id, JobPostingId = postingA.Id, Stage = ApplicationStage.Screening,
            StageChangedAt = now.AddDays(-10), AppliedAt = now.AddDays(-10),
        };
        var application3 = new JobApplication
        {
            CandidateId = candidate3.Id, JobPostingId = postingB.Id, Stage = ApplicationStage.Rejected,
            StageChangedAt = now.AddDays(-40), AppliedAt = now.AddDays(-40),
        };
        context.Applications.AddRange(application1, application2, application3);
        await context.SaveChangesAsync();

        context.ApplicationStageHistories.Add(new ApplicationStageHistory
        {
            ApplicationId = application1.Id, FromStage = ApplicationStage.OfferAccepted, ToStage = ApplicationStage.Hired,
            ChangedByUserId = Guid.NewGuid(), ChangedAt = now.AddDays(-5),
        });
        context.Offers.AddRange(
            new Offer { ApplicationId = application1.Id, Token = "token-a1", Status = OfferStatus.Accepted },
            new Offer { ApplicationId = application2.Id, Token = "token-a2", Status = OfferStatus.Draft },
            new Offer { ApplicationId = application3.Id, Token = "token-a3", Status = OfferStatus.Declined });
        await context.SaveChangesAsync();

        // ---- HRAdmin sees everything across both requisitions ----
        var hrReportingService = CreateReportingService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var hrReport = await hrReportingService.GetReportAsync();

        Assert.Equal(2, hrReport.Summary.OpenRequisitions);
        Assert.Equal(1, hrReport.Summary.ActiveApplications);
        Assert.Equal(1, hrReport.Summary.HiresLast30Days);
        Assert.Equal(15.0, hrReport.Summary.AverageTimeToHireDays);
        Assert.Equal(2, hrReport.Summary.OffersSent); // Draft offer excluded
        Assert.Equal(1, hrReport.Summary.OffersAccepted);
        Assert.Equal(50.0, hrReport.Summary.OfferAcceptanceRatePercent);
        Assert.Equal(50.0, hrReport.Summary.OfferToJoiningRatePercent);

        var careerSite = hrReport.SourceEffectiveness.Single(s => s.Source == CandidateSource.CareerSite);
        Assert.Equal(2, careerSite.Applications);
        Assert.Equal(1, careerSite.Hires);
        Assert.Equal(50.0, careerSite.ConversionRatePercent);
        var referral = hrReport.SourceEffectiveness.Single(s => s.Source == CandidateSource.Referral);
        Assert.Equal(1, referral.Applications);
        Assert.Equal(0, referral.Hires);

        Assert.Equal(2, hrReport.RequisitionAgeing.Count);
        Assert.Equal(requisitionA.Id, hrReport.RequisitionAgeing[0].RequisitionId); // oldest first
        Assert.True(hrReport.RequisitionAgeing[0].IsStale);
        Assert.False(hrReport.RequisitionAgeing[1].IsStale);

        var timeToHire = Assert.Single(hrReport.TimeToHireByPosting);
        Assert.Equal(postingA.Id, timeToHire.JobPostingId);
        Assert.Equal(1, timeToHire.Hires);
        Assert.Equal(15.0, timeToHire.AverageTimeToHireDays);

        // ---- Manager A (raised requisition A only) sees only their own slice ----
        var managerAReportingService = CreateReportingService(context, _managerUserId, []);
        var managerAReport = await managerAReportingService.GetReportAsync();

        Assert.Equal(1, managerAReport.Summary.OpenRequisitions);
        Assert.Equal(1, managerAReport.Summary.ActiveApplications); // application2 only
        Assert.Equal(1, managerAReport.Summary.OffersSent); // application3's offer is out of scope
        Assert.Equal(100.0, managerAReport.Summary.OfferAcceptanceRatePercent);
        Assert.Equal(100.0, managerAReport.Summary.OfferToJoiningRatePercent);
        Assert.Single(managerAReport.RequisitionAgeing);
        Assert.Equal(requisitionA.Id, managerAReport.RequisitionAgeing[0].RequisitionId);
    }

    private RecruitmentReportingService CreateReportingService(
        ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles) =>
        new(context, new TestCurrentUserService(userId, roles));

    [Fact]
    public async Task Referral_bonus_becomes_payable_on_hire_and_hr_can_mark_it_paid()
    {
        await using var context = CreateContext(_tenantId);
        await SeedTenantAsync(context);
        await CreateTenantSettingsService(context).UpdateAsync(new UpdateTenantSettingsRequest(180, 25000m, null));

        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);
        var posting = await PublishRequisitionAsync(managerService, hrService, "Referral Bonus Role");

        var referringEmployeeId = Guid.NewGuid();
        var referralService = new ReferralService(
            context, new TestCurrentUserService(Guid.NewGuid(), [], referringEmployeeId), careerSiteService);

        var referralResult = await referralService.SubmitReferralAsync(
            posting.Slug, new ReferralSubmissionRequest("Ada", "Lovelace", "ada.lovelace@example.com", "9999999980"),
            null, null, null, 0);
        Assert.True(referralResult.Succeeded);
        var applicationId = referralResult.ApplicationId!.Value;

        var hrOfferService = CreateOfferService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var offer = await hrOfferService.CreateAsync(
            applicationId, new CreateOrReviseOfferRequest("Engineer", new DateOnly(2026, 9, 1), 1200000m, null, null, null, null, null));
        await hrOfferService.SubmitForApprovalAsync(offer!.Id);
        await hrOfferService.ApproveAsync(offer.Id, new OfferDecisionRequest(null));
        await hrOfferService.SendAsync(offer.Id, new SendOfferRequest(7));
        var offerToken = await context.Offers.Where(o => o.ApplicationId == applicationId).Select(o => o.Token).SingleAsync();
        await hrOfferService.RespondPublicOfferAsync(offerToken, new PublicOfferDecisionRequest(true, null));

        var hrOnboardingService = CreateOnboardingService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var conversion = await hrOnboardingService.ConvertToEmployeeAsync(applicationId);
        Assert.True(conversion.Succeeded);

        var candidateId = await context.Candidates.AsNoTracking()
            .Where(c => c.Email == "ada.lovelace@example.com").Select(c => c.Id).SingleAsync();
        var candidate = await context.Candidates.AsNoTracking().SingleAsync(c => c.Id == candidateId);
        Assert.Equal(ReferralBonusStatus.Payable, candidate.ReferralBonusStatus);
        Assert.Equal(25000m, candidate.ReferralBonusAmount);

        var hrReferralService = new ReferralService(
            context, new TestCurrentUserService(Guid.NewGuid(), [RoleNames.HRAdmin]), careerSiteService);
        var payouts = await hrReferralService.GetPayoutsAsync();
        var payout = Assert.Single(payouts);
        Assert.Equal(candidateId, payout.CandidateId);
        Assert.Equal(ReferralBonusStatus.Payable, payout.Status);

        Assert.True(await hrReferralService.MarkBonusPaidAsync(candidateId));
        var candidateAfterPaid = await context.Candidates.AsNoTracking().SingleAsync(c => c.Id == candidateId);
        Assert.Equal(ReferralBonusStatus.Paid, candidateAfterPaid.ReferralBonusStatus);
        Assert.NotNull(candidateAfterPaid.ReferralBonusPaidAt);

        // Already Paid - can't be marked paid a second time.
        Assert.False(await hrReferralService.MarkBonusPaidAsync(candidateId));
    }

    [Fact]
    public async Task Offer_letter_renders_the_tenant_template_with_merge_variables_when_configured()
    {
        await using var context = CreateContext(_tenantId);
        await SeedTenantAsync(context);
        await CreateTenantSettingsService(context).UpdateAsync(new UpdateTenantSettingsRequest(
            180, null, "Dear {{CandidateName}}, welcome to {{CompanyName}} as {{Designation}} at {{AnnualCtc}} per year."));

        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);
        var posting = await PublishRequisitionAsync(managerService, hrService, "Template Test Role");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, new PublicApplicationRequest("Katherine", "Johnson", "katherine@example.com", "9999999979", null, null, null, true),
            CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var applicationId = applyResult.ApplicationId!.Value;

        var hrOfferService = CreateOfferService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var offer = await hrOfferService.CreateAsync(
            applicationId, new CreateOrReviseOfferRequest("Senior Engineer", new DateOnly(2026, 9, 1), 1500000m, null, null, null, null, null));

        var offerLetterText = await context.Offers.AsNoTracking()
            .Where(o => o.Id == offer!.Id)
            .SelectMany(o => o.Versions)
            .Select(v => v.OfferLetterText)
            .SingleAsync();

        Assert.Contains("Dear Katherine Johnson", offerLetterText);
        Assert.Contains("Test Tenant", offerLetterText);
        Assert.Contains("Senior Engineer", offerLetterText);
        Assert.Contains(1500000m.ToString("N0"), offerLetterText);
    }

    [Fact]
    public async Task Candidate_can_self_serve_a_deletion_request_and_hr_can_approve_it()
    {
        await using var context = CreateContext(_tenantId);
        await SeedTenantAsync(context);

        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);
        var posting = await PublishRequisitionAsync(managerService, hrService, "Privacy Test Role");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        await careerSiteService.ApplyAsync(
            posting.Slug, new PublicApplicationRequest("Rejected", "Person", "rejected.person@example.com", "9999999978", null, null, null, true),
            CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var candidateId = await context.Candidates.AsNoTracking()
            .Where(c => c.Email == "rejected.person@example.com").Select(c => c.Id).SingleAsync();

        var candidateDataPrivacyService = CreateDataPrivacyService(context, Guid.NewGuid(), [], candidateId);
        Assert.True((await candidateDataPrivacyService.RequestDeletionAsync()).Succeeded);

        // Can't submit a second request while one is already pending.
        Assert.False((await candidateDataPrivacyService.RequestDeletionAsync()).Succeeded);

        var myRequest = await candidateDataPrivacyService.GetMyDeletionRequestAsync();
        Assert.Equal(CandidateDataDeletionRequestStatus.Pending, myRequest!.Status);

        var hrDataPrivacyService = CreateDataPrivacyService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var pendingRequest = Assert.Single(
            await hrDataPrivacyService.GetDeletionRequestsAsync(CandidateDataDeletionRequestStatus.Pending));

        var decision = await hrDataPrivacyService.DecideDeletionRequestAsync(
            pendingRequest.Id, new DecideDeletionRequestRequest(true, "Confirmed identity, approving erasure."));
        Assert.True(decision.Succeeded);

        var anonymizedCandidate = await context.Candidates.AsNoTracking().SingleAsync(c => c.Id == candidateId);
        Assert.True(anonymizedCandidate.IsAnonymized);
        Assert.Equal("Redacted", anonymizedCandidate.FirstName);
        Assert.StartsWith("redacted-", anonymizedCandidate.Email);
        Assert.Null(anonymizedCandidate.ResumeDocumentId);
    }

    [Fact]
    public async Task Deletion_request_is_refused_once_the_candidate_has_been_hired()
    {
        await using var context = CreateContext(_tenantId);
        await SeedTenantAsync(context);

        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var careerSiteService = CreateCareerSiteService(context);
        var posting = await PublishRequisitionAsync(managerService, hrService, "Hired Privacy Role");

        using var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray());
        var applyResult = await careerSiteService.ApplyAsync(
            posting.Slug, new PublicApplicationRequest("Soon", "Hired", "soon.hired@example.com", "9999999977", null, null, null, true),
            CandidateSource.CareerSite, null, resume, "resume.pdf", "application/pdf", resume.Length);
        var applicationId = applyResult.ApplicationId!.Value;
        var candidateId = await context.Candidates.AsNoTracking()
            .Where(c => c.Email == "soon.hired@example.com").Select(c => c.Id).SingleAsync();

        var hrOfferService = CreateOfferService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var offer = await hrOfferService.CreateAsync(
            applicationId, new CreateOrReviseOfferRequest("Engineer", new DateOnly(2026, 9, 1), 1000000m, null, null, null, null, null));
        await hrOfferService.SubmitForApprovalAsync(offer!.Id);
        await hrOfferService.ApproveAsync(offer.Id, new OfferDecisionRequest(null));
        await hrOfferService.SendAsync(offer.Id, new SendOfferRequest(7));
        var offerToken = await context.Offers.Where(o => o.ApplicationId == applicationId).Select(o => o.Token).SingleAsync();
        await hrOfferService.RespondPublicOfferAsync(offerToken, new PublicOfferDecisionRequest(true, null));

        var hrOnboardingService = CreateOnboardingService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        Assert.True((await hrOnboardingService.ConvertToEmployeeAsync(applicationId)).Succeeded);

        var candidateDataPrivacyService = CreateDataPrivacyService(context, Guid.NewGuid(), [], candidateId);
        Assert.True((await candidateDataPrivacyService.RequestDeletionAsync()).Succeeded);

        var hrDataPrivacyService = CreateDataPrivacyService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var pendingRequest = Assert.Single(
            await hrDataPrivacyService.GetDeletionRequestsAsync(CandidateDataDeletionRequestStatus.Pending));

        var decision = await hrDataPrivacyService.DecideDeletionRequestAsync(
            pendingRequest.Id, new DecideDeletionRequestRequest(true, null));
        Assert.False(decision.Succeeded);

        var candidateAfter = await context.Candidates.AsNoTracking().SingleAsync(c => c.Id == candidateId);
        Assert.False(candidateAfter.IsAnonymized);
    }

    [Fact]
    public async Task Retention_sweep_anonymizes_stale_rejected_candidates_but_exempts_recent_and_talent_pool_ones()
    {
        await using var context = CreateContext(_tenantId);
        await SeedTenantAsync(context); // default 180-day retention
        var now = DateTimeOffset.UtcNow;

        var managerService = CreateRequisitionService(context, _managerUserId, []);
        var hrService = CreateRequisitionService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        var posting = await PublishRequisitionAsync(managerService, hrService, "Sweep Test Role");

        var staleRejected = new Candidate
        {
            FirstName = "Stale", LastName = "Rejected", Email = "stale.rejected@example.com", Phone = "9000000010",
            Source = CandidateSource.CareerSite, ConsentGivenAt = now.AddDays(-200),
        };
        var recentRejected = new Candidate
        {
            FirstName = "Recent", LastName = "Rejected", Email = "recent.rejected@example.com", Phone = "9000000011",
            Source = CandidateSource.CareerSite, ConsentGivenAt = now.AddDays(-10),
        };
        var talentPoolRejected = new Candidate
        {
            FirstName = "TalentPool", LastName = "Rejected", Email = "talentpool.rejected@example.com", Phone = "9000000012",
            Source = CandidateSource.CareerSite, ConsentGivenAt = now.AddDays(-200), IsInTalentPool = true,
        };
        context.Candidates.AddRange(staleRejected, recentRejected, talentPoolRejected);
        await context.SaveChangesAsync();

        context.Applications.AddRange(
            new JobApplication
            {
                CandidateId = staleRejected.Id, JobPostingId = posting.Id, Stage = ApplicationStage.Rejected,
                StageChangedAt = now.AddDays(-200), AppliedAt = now.AddDays(-210),
            },
            new JobApplication
            {
                CandidateId = recentRejected.Id, JobPostingId = posting.Id, Stage = ApplicationStage.Rejected,
                StageChangedAt = now.AddDays(-10), AppliedAt = now.AddDays(-20),
            },
            new JobApplication
            {
                CandidateId = talentPoolRejected.Id, JobPostingId = posting.Id, Stage = ApplicationStage.Rejected,
                StageChangedAt = now.AddDays(-200), AppliedAt = now.AddDays(-210),
            });
        await context.SaveChangesAsync();

        var hrDataPrivacyService = CreateDataPrivacyService(context, Guid.NewGuid(), [RoleNames.HRAdmin]);
        Assert.Equal(1, await hrDataPrivacyService.RunRetentionSweepAsync());

        Assert.True((await context.Candidates.AsNoTracking().SingleAsync(c => c.Id == staleRejected.Id)).IsAnonymized);
        Assert.False((await context.Candidates.AsNoTracking().SingleAsync(c => c.Id == recentRejected.Id)).IsAnonymized);
        Assert.False((await context.Candidates.AsNoTracking().SingleAsync(c => c.Id == talentPoolRejected.Id)).IsAnonymized);
    }

    private async Task<Domain.Tenancy.Tenant> SeedTenantAsync(ApplicationDbContext context)
    {
        var tenant = new Domain.Tenancy.Tenant { Id = _tenantId, Name = "Test Tenant", Slug = $"test-{_tenantId:N}" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant;
    }

    private TenantSettingsService CreateTenantSettingsService(ApplicationDbContext context) =>
        new(context, new TestTenantContext(_tenantId));

    private DataPrivacyService CreateDataPrivacyService(
        ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles, Guid? candidateId = null) =>
        new(context, new TestTenantContext(_tenantId), new TestCurrentUserService(userId, roles),
            new TestCandidateContext(candidateId), new LocalFileStorageService(new TestFileStorageOptions(_storageRoot)));

    private OnboardingService CreateOnboardingService(
        ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles,
        Guid? employeeId = null, IUserDirectory? userDirectory = null)
    {
        var effectiveUserDirectory = userDirectory ?? new NoOpUserDirectory();
        return new OnboardingService(
            context, new TestCurrentUserService(userId, roles, employeeId), effectiveUserDirectory,
            new EmployeeService(context, new SequenceGenerator(context, new TestTenantContext(_tenantId)),
                new TestCurrentUserService(userId, roles, employeeId)));
    }

    private CandidatePortalAuthService CreateCandidatePortalAuthService(ApplicationDbContext context, IEmailSender emailSender) =>
        new(context, new TestTenantContext(_tenantId), emailSender, new JwtTokenGenerator(new TestJwtOptions()),
            NullLogger<CandidatePortalAuthService>.Instance);

    private OfferService CreateOfferService(
        ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles, IEmailSender? preboardingEmailSender = null) =>
        new(context, new TestTenantContext(_tenantId), new TestCurrentUserService(userId, roles),
            new NoOpFrontendLinkBuilder(), new NoOpEmailSender(),
            CreatePreboardingService(context, userId, roles, emailSender: preboardingEmailSender),
            NullLogger<OfferService>.Instance);

    private PreboardingService CreatePreboardingService(
        ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles,
        Guid? candidateId = null, IEmailSender? emailSender = null) =>
        new(context, new TestTenantContext(_tenantId), new TestCurrentUserService(userId, roles),
            new TestCandidateContext(candidateId), new LocalFileStorageService(new TestFileStorageOptions(_storageRoot)),
            emailSender ?? new NoOpEmailSender(), NullLogger<PreboardingService>.Instance);

    private BackgroundVerificationService CreateBgvService(ApplicationDbContext context, Guid userId, IReadOnlyCollection<string> roles) =>
        new(context, new TestCurrentUserService(userId, roles));

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
    private class NoOpUserDirectory(IReadOnlyDictionary<Guid, Guid>? userIdToEmployeeId = null) : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(userIds.ToDictionary(id => id, _ => "Test User"));

        public Task<Guid?> GetEmployeeIdForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userIdToEmployeeId is not null && userIdToEmployeeId.TryGetValue(userId, out var employeeId)
                ? employeeId
                : (Guid?)null);
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
