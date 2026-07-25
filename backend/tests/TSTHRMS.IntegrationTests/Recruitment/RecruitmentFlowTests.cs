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
            published.JobPosting.Slug, applyRequest, CandidateSource.CareerSite,
            resume, "resume.pdf", "application/pdf", resume.Length);
        Assert.True(firstApply.Succeeded);
        Assert.NotNull(firstApply.ApplicationId);

        // Re-applying to the same posting with the same email/phone is rejected, not duplicated.
        resume.Position = 0;
        var duplicateApply = await careerSiteService.ApplyAsync(
            published.JobPosting.Slug, applyRequest, CandidateSource.CareerSite,
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
                firstPosting.Slug, applyRequest, CandidateSource.CareerSite, resume, "resume.pdf", "application/pdf", resume.Length);
            Assert.True(firstApply.Succeeded);
        }

        using (var resume = new MemoryStream("%PDF-1.4 resume"u8.ToArray()))
        {
            var secondApply = await careerSiteService.ApplyAsync(
                secondPosting.Slug, applyRequest, CandidateSource.CareerSite, resume, "resume.pdf", "application/pdf", resume.Length);
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
            posting.Slug, applyRequest, CandidateSource.CareerSite, resume, "resume.pdf", "application/pdf", resume.Length);
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

    private class TestCurrentUserService(Guid? userId, IReadOnlyCollection<string> roles) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public IReadOnlyCollection<string> Roles => roles;
    }

    private class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
}
