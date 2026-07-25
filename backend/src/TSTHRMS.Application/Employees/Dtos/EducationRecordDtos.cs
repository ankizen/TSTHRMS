using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Dtos;

public record EducationRecordDto(
    Guid Id,
    Guid EmployeeId,
    QualificationLevel QualificationLevel,
    string DegreeName,
    string InstituteName,
    int YearOfPassing,
    string? Specialization,
    VerificationStatus VerificationStatus,
    Guid? CertificateDocumentId,
    string? CertificateFileName);

public record EducationRecordWriteRequest(
    QualificationLevel QualificationLevel,
    string DegreeName,
    string InstituteName,
    int YearOfPassing,
    string? Specialization);

public record UpdateVerificationStatusRequest(VerificationStatus VerificationStatus);

public record AttachCertificateResult(bool Succeeded, EducationRecordDto? Record, string? Error)
{
    public static AttachCertificateResult Success(EducationRecordDto record) => new(true, record, null);
    public static AttachCertificateResult Failure(string error) => new(false, null, error);
}
