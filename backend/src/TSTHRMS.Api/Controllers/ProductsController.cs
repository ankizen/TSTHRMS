using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Tenancy.Dtos;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController(IApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LookupDto>>> GetList(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .OrderBy(p => p.Name)
            .Select(p => new LookupDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }
}
