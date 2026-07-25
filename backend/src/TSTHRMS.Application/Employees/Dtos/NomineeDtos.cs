using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Dtos;

public record NomineeDto(
    Guid Id,
    Guid EmployeeId,
    NominationType NominationType,
    string Name,
    string Relation,
    decimal? SharePercentage,
    string? ContactNumber,
    Guid? FamilyMemberId,
    string? FamilyMemberName,
    Guid? ConsentDocumentId,
    string? ConsentFileName);

public record NomineeWriteRequest(
    NominationType NominationType,
    string Name,
    string Relation,
    decimal? SharePercentage,
    string? ContactNumber,
    Guid? FamilyMemberId);

public record NomineeUpsertResult(bool Succeeded, NomineeDto? Record, string? Error)
{
    public static NomineeUpsertResult Success(NomineeDto record) => new(true, record, null);
    public static NomineeUpsertResult Failure(string error) => new(false, null, error);
}
