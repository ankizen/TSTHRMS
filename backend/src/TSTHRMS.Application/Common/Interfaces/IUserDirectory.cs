namespace TSTHRMS.Application.Common.Interfaces;

/// <summary>
/// Resolves user ids (e.g. AuditLog.ChangedByUserId) to a human-readable label for display.
/// Kept as its own small abstraction rather than exposing ApplicationUser through
/// IApplicationDbContext, since Identity types belong to Infrastructure, not Application.
/// </summary>
public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>Recruitment Section 11: resolves a requisition's raising hiring manager (a
    /// login/UserId) to their own Employee record, so a converted hire's ReportingManagerId can
    /// be auto-set. Null if the user has no linked employee.</summary>
    Task<Guid?> GetEmployeeIdForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
