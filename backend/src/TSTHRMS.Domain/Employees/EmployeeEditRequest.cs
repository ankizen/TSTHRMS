using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Employees;

/// <summary>
/// Core HR Section 14: an Employee's self-service record is read-only, so a change goes through
/// this request/review queue instead of a direct write. Deliberately limited to a small whitelist
/// of simple string fields (contact/address/emergency-contact details) - applying an approved
/// request is a plain switch over EditableEmployeeField, not a generic reflection-based setter,
/// so there's no way for a request to target a field it wasn't meant to.
/// </summary>
public class EmployeeEditRequest : TenantScopedEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public EditableEmployeeField Field { get; set; }
    public string? OldValue { get; set; }
    public required string NewValue { get; set; }

    public EditRequestStatus Status { get; set; } = EditRequestStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public enum EditableEmployeeField
{
    PersonalEmail,
    PersonalPhone,
    CurrentAddress,
    PermanentAddress,
    EmergencyContactName,
    EmergencyContactRelation,
    EmergencyContactPhone
}

public enum EditRequestStatus
{
    Pending,
    Approved,
    Rejected
}
