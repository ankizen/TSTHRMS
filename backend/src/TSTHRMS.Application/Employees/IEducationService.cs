using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public interface IEducationService
{
    /// <summary>Ordered highest qualification first.</summary>
    Task<IReadOnlyList<EducationRecordDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<EducationRecordDto?> CreateAsync(Guid employeeId, EducationRecordWriteRequest request, CancellationToken cancellationToken = default);

    Task<EducationRecordDto?> UpdateAsync(Guid employeeId, Guid id, EducationRecordWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default);

    Task<EducationRecordDto?> UpdateVerificationStatusAsync(Guid employeeId, Guid id, VerificationStatus status, CancellationToken cancellationToken = default);

    /// <summary>Null return means the employee/education record wasn't found; a Failure result
    /// means it was found but the file didn't pass size/type validation.</summary>
    Task<AttachCertificateResult?> AttachCertificateAsync(
        Guid employeeId, Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);
}
