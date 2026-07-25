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
