using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.BackgroundJobs;

/// <summary>
/// ITenantContext for a background job's own manually-constructed ApplicationDbContext - a
/// background service has no HttpContext, so the request-scoped Api.Services.TenantContext can't
/// resolve anything. Same shape as the integration test suite's TestTenantContext.
/// </summary>
public class StaticTenantContext(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;
    public bool IsResolved => tenantId != Guid.Empty;
}
