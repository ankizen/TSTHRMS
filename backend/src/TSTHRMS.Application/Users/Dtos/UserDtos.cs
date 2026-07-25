namespace TSTHRMS.Application.Users.Dtos;

public record UserSummaryDto(
    Guid Id,
    string Email,
    IReadOnlyList<string> Roles,
    Guid? EmployeeId,
    string? EmployeeName,
    Guid? AssignedLegalEntityId,
    string? AssignedLegalEntityName,
    Guid? AssignedProductId,
    string? AssignedProductName);

/// <summary>Role is a plain string validated against RoleNames.All rather than the enum-like
/// constants themselves, since it arrives from JSON and there's no natural enum backing a set of
/// string constants.</summary>
public record CreateUserRequest(
    Guid EmployeeId,
    string Email,
    string Password,
    string Role,
    Guid? AssignedLegalEntityId,
    Guid? AssignedProductId);

public record UserCreationResult(bool Succeeded, UserSummaryDto? User, IReadOnlyList<string> Errors)
{
    public static UserCreationResult Success(UserSummaryDto user) => new(true, user, []);
    public static UserCreationResult Failure(IEnumerable<string> errors) => new(false, null, errors.ToList());
}
