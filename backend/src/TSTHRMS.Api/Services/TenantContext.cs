using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Api.Services;

public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid TenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return claim is not null && Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }

    public bool IsResolved => TenantId != Guid.Empty;
}
