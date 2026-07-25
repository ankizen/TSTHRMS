namespace TSTHRMS.Application.Employees.Dtos;

public record PreviousEmploymentRecordDto(
    Guid Id,
    Guid EmployeeId,
    string CompanyName,
    string? Designation,
    decimal? YearsOfExperience,
    DateOnly DateOfJoining,
    DateOnly DateOfLeaving,
    string? ReasonForLeaving,
    string? PreviousUan,
    Guid? RelievingLetterDocumentId,
    string? RelievingLetterFileName,
    Guid? LastSalarySlipDocumentId,
    string? LastSalarySlipFileName);

public record PreviousEmploymentRecordWriteRequest(
    string CompanyName,
    string? Designation,
    decimal? YearsOfExperience,
    DateOnly DateOfJoining,
    DateOnly DateOfLeaving,
    string? ReasonForLeaving,
    string? PreviousUan);
