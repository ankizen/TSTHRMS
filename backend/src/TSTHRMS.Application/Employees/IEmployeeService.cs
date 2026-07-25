using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeListItemDto>> GetListAsync(
        int page, int pageSize, string? search, EmployeeStatus? status, CancellationToken cancellationToken = default);

    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EmployeeDto> CreateAsync(EmployeeWriteRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeDto?> UpdateAsync(Guid id, EmployeeWriteRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeDto?> UpdateStatusAsync(Guid id, EmployeeStatus status, CancellationToken cancellationToken = default);

    /// <summary>Returns the unmasked bank account number and writes a logged Revealed audit entry.
    /// Null return means the employee wasn't found.</summary>
    Task<BankAccountRevealDto?> RevealBankAccountNumberAsync(Guid id, CancellationToken cancellationToken = default);
}
