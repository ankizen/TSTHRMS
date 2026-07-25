using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

public interface IJobRequisitionService
{
    /// <summary>A Manager (raised without HRAdmin/HRBP) only ever sees their own requisitions -
    /// the same scoping shape as EmployeeService.ApplyHrbpScope for HRBP.</summary>
    Task<IReadOnlyList<JobRequisitionListItemDto>> GetListAsync(
        RequisitionStatus? status, CancellationToken cancellationToken = default);

    Task<JobRequisitionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<JobRequisitionDto> CreateAsync(JobRequisitionWriteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Null means not found, or found but not owned by the caller and status isn't
    /// Draft/Rejected. Only ever mutates Draft/Rejected requisitions - resets Rejected back to
    /// Draft since the fields changed.</summary>
    Task<JobRequisitionDto?> UpdateAsync(
        Guid id, JobRequisitionWriteRequest request, CancellationToken cancellationToken = default);

    Task<JobRequisitionDto?> SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken = default);

    Task<JobRequisitionDto?> DecideAsync(
        Guid id, RequisitionApprovalDecision decision, RequisitionDecisionRequest request,
        CancellationToken cancellationToken = default);

    Task<JobRequisitionDto?> PutOnHoldAsync(Guid id, CancellationToken cancellationToken = default);

    Task<JobRequisitionDto?> ResumeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<JobRequisitionDto?> CloseAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Approved -> creates (first time) or re-publishes (after a hold) the JobPosting.
    /// Description/Location are required only when no JobPosting exists yet.</summary>
    Task<JobRequisitionDto?> PublishAsync(
        Guid id, PublishJobPostingRequest request, CancellationToken cancellationToken = default);
}
