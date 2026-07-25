using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Dtos;

public record SubmitEditRequestItem(EditableEmployeeField Field, string? NewValue);

public record SubmitEditRequestsRequest(IReadOnlyList<SubmitEditRequestItem> Changes);

public record ReviewEditRequestDto(string? ReviewNote);

public record EmployeeEditRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    EditableEmployeeField Field,
    string? OldValue,
    string NewValue,
    EditRequestStatus Status,
    Guid? ReviewedByUserId,
    string? ReviewedByDisplayName,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote,
    DateTimeOffset CreatedAt);
