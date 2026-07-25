using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public interface IPreviousEmploymentService
{
    Task<IReadOnlyList<PreviousEmploymentRecordDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<PreviousEmploymentRecordDto?> CreateAsync(Guid employeeId, PreviousEmploymentRecordWriteRequest request, CancellationToken cancellationToken = default);

    Task<PreviousEmploymentRecordDto?> UpdateAsync(Guid employeeId, Guid id, PreviousEmploymentRecordWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Null return means the employee/record wasn't found; a Failure result means it
    /// was found but the file didn't pass size/type validation.</summary>
    Task<AttachDocumentResult<PreviousEmploymentRecordDto>?> AttachDocumentAsync(
        Guid employeeId, Guid id, PreviousEmploymentDocumentSlot slot,
        Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);
}
