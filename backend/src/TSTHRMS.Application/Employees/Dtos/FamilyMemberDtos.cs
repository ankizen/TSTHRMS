using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Dtos;

public record FamilyMemberDto(
    Guid Id,
    Guid EmployeeId,
    FamilyRelation Relation,
    string Name,
    DateOnly? DateOfBirth,
    bool IsDependent,
    bool IsDifferentlyAbled);

public record FamilyMemberWriteRequest(
    FamilyRelation Relation,
    string Name,
    DateOnly? DateOfBirth,
    bool IsDependent,
    bool IsDifferentlyAbled);
