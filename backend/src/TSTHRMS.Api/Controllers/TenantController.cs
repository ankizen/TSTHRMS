using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Api.Controllers;

/// <summary>Lets the authenticated app know its own tenant's public Slug - needed to build the
/// shareable Career Site link (/careers/{slug}) shown on the Requisitions screen.</summary>
[ApiController]
[Route("api/tenant")]
[Authorize]
public class TenantController(IApplicationDbContext dbContext, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<TenantSummaryDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants
            .Where(t => t.Id == tenantContext.TenantId)
            .Select(t => new TenantSummaryDto(t.Id, t.Name, t.Slug))
            .FirstOrDefaultAsync(cancellationToken);

        return tenant is null ? NotFound() : Ok(tenant);
    }
}

public record TenantSummaryDto(Guid Id, string Name, string Slug);
