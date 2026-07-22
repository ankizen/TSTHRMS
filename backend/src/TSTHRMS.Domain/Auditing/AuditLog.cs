using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Auditing;

/// <summary>
/// Read-only change-history record. Stores the real old/new values (compliance needs the
/// true history) - masking sensitive fields for display is an API/UI concern applied on read.
/// </summary>
public class AuditLog : TenantScopedEntity
{
    public required string EntityName { get; set; }
    public required string EntityId { get; set; }
    public required AuditAction Action { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public required string ChangesJson { get; set; }
}

public enum AuditAction
{
    Created,
    Updated,
    Deleted
}

public record AuditFieldChange(string PropertyName, string? OldValue, string? NewValue, bool IsSensitive);
