using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Domain.Auditing;
using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.CustomFields;
using TSTHRMS.Domain.Documents;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Recruitment;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Identity;

namespace TSTHRMS.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext
{
    private static readonly MethodInfo SetTenantFilterMethod = typeof(ApplicationDbContext)
        .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo SetTenantAndSoftDeleteFilterMethod = typeof(ApplicationDbContext)
        .GetMethod(nameof(SetTenantAndSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EducationRecord> EducationRecords => Set<EducationRecord>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<PreviousEmploymentRecord> PreviousEmploymentRecords => Set<PreviousEmploymentRecord>();
    public DbSet<IdentityDocument> IdentityDocuments => Set<IdentityDocument>();
    public DbSet<Nominee> Nominees => Set<Nominee>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<EmployeeEditRequest> EmployeeEditRequests => Set<EmployeeEditRequest>();
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<EmployeeCustomFieldValue> EmployeeCustomFieldValues => Set<EmployeeCustomFieldValue>();
    public DbSet<TenantSequence> TenantSequences => Set<TenantSequence>();
    public DbSet<JobRequisition> JobRequisitions => Set<JobRequisition>();
    public DbSet<RequisitionApproval> RequisitionApprovals => Set<RequisitionApproval>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<ApplicationStageHistory> ApplicationStageHistories => Set<ApplicationStageHistory>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewPanelist> InterviewPanelists => Set<InterviewPanelist>();
    public DbSet<InterviewScorecard> InterviewScorecards => Set<InterviewScorecard>();
    public DbSet<AssessmentSubmission> AssessmentSubmissions => Set<AssessmentSubmission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Every ITenantScoped entity is automatically filtered by the current tenant -
        // wired into the model convention (not a per-entity opt-in) so a new table can
        // never accidentally leak across tenants by a developer forgetting to add a filter.
        // ISoftDeletable entities (Section 15's Education/Family/PreviousEmployment/Identity
        // Document/Nominee - never hard-deleted) get IsDeleted folded into the same filter.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isTenantScoped = typeof(ITenantScoped).IsAssignableFrom(clrType);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clrType);

            if (isTenantScoped && isSoftDeletable)
            {
                SetTenantAndSoftDeleteFilterMethod.MakeGenericMethod(clrType).Invoke(this, [builder]);
            }
            else if (isTenantScoped)
            {
                SetTenantFilterMethod.MakeGenericMethod(clrType).Invoke(this, [builder]);
            }
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantScoped
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == tenantContext.TenantId);
    }

    private void SetTenantAndSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantScoped, ISoftDeletable
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == tenantContext.TenantId && !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    if (entry.Entity is ITenantScoped scoped && scoped.TenantId == Guid.Empty && tenantContext.IsResolved)
                    {
                        scoped.TenantId = tenantContext.TenantId;
                    }
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
