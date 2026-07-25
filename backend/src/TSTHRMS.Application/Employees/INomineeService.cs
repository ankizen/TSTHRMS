using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees;

public interface INomineeService
{
    Task<IReadOnlyList<NomineeDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>Null return means the employee wasn't found; a Failure result means either the
    /// linked family member doesn't belong to this employee, or the share percentage would
    /// push the nomination type's total over 100%.</summary>
    Task<NomineeUpsertResult?> CreateAsync(Guid employeeId, NomineeWriteRequest request, CancellationToken cancellationToken = default);

    Task<NomineeUpsertResult?> UpdateAsync(Guid employeeId, Guid id, NomineeWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default);

    Task<AttachDocumentResult<NomineeDto>?> AttachConsentDocumentAsync(
        Guid employeeId, Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);
}
