using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees;

/// <summary>Section 14: the Employee and Manager self-service surfaces - always keyed off the
/// caller's own employee_id claim, never a route parameter, so there's no id to spoof.</summary>
public interface IMyProfileService
{
    /// <summary>Null means the caller has no linked Employee record.</summary>
    Task<EmployeeDto?> GetOwnProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>Restricted-field view of everyone reporting to the caller. Empty if the caller
    /// has no linked Employee record or no direct reports.</summary>
    Task<IReadOnlyList<DirectReportSummaryDto>> GetDirectReportsAsync(CancellationToken cancellationToken = default);
}
