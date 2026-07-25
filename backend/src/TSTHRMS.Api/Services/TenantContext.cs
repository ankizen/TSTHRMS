using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Api.Services;

public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    /// <summary>Key an anonymous public endpoint (career site) stashes the tenant it resolved by
    /// slug into - see <see cref="TSTHRMS.Api.Filters.ResolvePublicTenantAttribute"/>. Checked
    /// before the JWT claim so the same query filters/auto-stamping work for anonymous requests
    /// without touching the ITenantContext interface itself.</summary>
    public const string PublicTenantItemsKey = "tenant_id_override";

    public Guid TenantId
    {
        get
        {
            if (httpContextAccessor.HttpContext?.Items[PublicTenantItemsKey] is Guid overrideId)
            {
                return overrideId;
            }

            var claim = httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return claim is not null && Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }

    public bool IsResolved => TenantId != Guid.Empty;
}
