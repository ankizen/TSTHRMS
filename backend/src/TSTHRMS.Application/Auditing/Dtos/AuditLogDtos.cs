using TSTHRMS.Domain.Auditing;

namespace TSTHRMS.Application.Auditing.Dtos;

/// <summary>Masked by default when IsSensitive - see AuditLogService.ToDto. The unmasked values
/// are only ever returned via the explicitly-logged reveal endpoint.</summary>
public record AuditFieldChangeDto(string PropertyName, string? OldValue, string? NewValue, bool IsSensitive);

public record AuditLogEntryDto(
    Guid Id,
    string EntityName,
    string EntityId,
    AuditAction Action,
    Guid? ChangedByUserId,
    string? ChangedByDisplayName,
    DateTimeOffset ChangedAt,
    IReadOnlyList<AuditFieldChangeDto> Changes);
