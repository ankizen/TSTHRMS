using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;
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

    private class TestFileStorageOptions(string rootPath) : Microsoft.Extensions.Options.IOptions<LocalFileStorageOptions>
    {
        public LocalFileStorageOptions Value { get; } = new() { RootPath = rootPath };
    }
}
