using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees;

/// <summary>Section 14: an Employee's own record is read-only, so a change to the whitelisted
/// self-service fields goes through this request/review queue instead of a direct write.</summary>
public interface IEmployeeEditRequestService
{
    /// <summary>Submitted by the caller for their own linked Employee record. Empty result means
    /// the caller has no linked Employee record.</summary>
    Task<IReadOnlyList<EmployeeEditRequestDto>> SubmitAsync(
        SubmitEditRequestsRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeEditRequestDto>> GetOwnRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>HR review queue - respects HRBP scope like everything else in EmployeeService.</summary>
    Task<IReadOnlyList<EmployeeEditRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies the requested change to the Employee record. Null means the request
    /// wasn't found, was already reviewed, or is outside a scoped HRBP's assignment.</summary>
    Task<EmployeeEditRequestDto?> ApproveAsync(
        Guid requestId, ReviewEditRequestDto request, CancellationToken cancellationToken = default);

    Task<EmployeeEditRequestDto?> RejectAsync(
        Guid requestId, ReviewEditRequestDto request, CancellationToken cancellationToken = default);
}
