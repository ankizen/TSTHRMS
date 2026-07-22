namespace TSTHRMS.Application.Common.Interfaces;

/// <summary>
/// Resolves the tenant for the current request from the authenticated user's JWT claim.
/// Scoped per-request; the DbContext's global query filters key off this.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsResolved { get; }
}
