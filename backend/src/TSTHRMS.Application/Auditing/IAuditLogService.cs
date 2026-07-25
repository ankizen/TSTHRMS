using TSTHRMS.Application.Auditing.Dtos;

namespace TSTHRMS.Application.Auditing;

public interface IAuditLogService
{
    /// <summary>Every captured change to the employee's own record plus every child record that
    /// belongs to them (education, family, previous employment, identity documents, nominees,
    /// standalone documents) - one unified timeline. Null means the employee wasn't found.
    /// Sensitive field values come back masked.</summary>
    Task<IReadOnlyList<AuditLogEntryDto>?> GetEmployeeHistoryAsync(
        Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>Returns the same entry with sensitive field values unmasked, and writes a logged
    /// Revealed audit entry - same "mask by default, reveal is an audited action" rule used for
    /// the bank account field elsewhere. Null means the entry wasn't found or doesn't belong to
    /// this employee.</summary>
    Task<AuditLogEntryDto?> RevealEntryAsync(
        Guid employeeId, Guid auditLogId, CancellationToken cancellationToken = default);
}
