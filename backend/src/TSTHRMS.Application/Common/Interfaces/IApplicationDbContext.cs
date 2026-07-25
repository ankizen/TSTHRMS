using Microsoft.EntityFrameworkCore;
using TSTHRMS.Domain.Auditing;
using TSTHRMS.Domain.CustomFields;
using TSTHRMS.Domain.Documents;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Recruitment;
using TSTHRMS.Domain.Tenancy;

namespace TSTHRMS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<LegalEntity> LegalEntities { get; }
    DbSet<Product> Products { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Employee> Employees { get; }
    DbSet<EducationRecord> EducationRecords { get; }
    DbSet<FamilyMember> FamilyMembers { get; }
    DbSet<PreviousEmploymentRecord> PreviousEmploymentRecords { get; }
    DbSet<IdentityDocument> IdentityDocuments { get; }
    DbSet<Nominee> Nominees { get; }
    DbSet<Document> Documents { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<EmployeeEditRequest> EmployeeEditRequests { get; }
    DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; }
    DbSet<EmployeeCustomFieldValue> EmployeeCustomFieldValues { get; }
    DbSet<JobRequisition> JobRequisitions { get; }
    DbSet<RequisitionApproval> RequisitionApprovals { get; }
    DbSet<JobPosting> JobPostings { get; }
    DbSet<Candidate> Candidates { get; }
    DbSet<JobApplication> Applications { get; }
    DbSet<ApplicationStageHistory> ApplicationStageHistories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
