using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Tenancy.Dtos;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/legal-entities")]
[Authorize]
public class LegalEntitiesController(IApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LookupDto>>> GetList(CancellationToken cancellationToken)
    {
        var legalEntities = await dbContext.LegalEntities
            .OrderBy(e => e.Name)
            .Select(e => new LookupDto(e.Id, e.Name))
            .ToListAsync(cancellationToken);

        return Ok(legalEntities);
    }
}
