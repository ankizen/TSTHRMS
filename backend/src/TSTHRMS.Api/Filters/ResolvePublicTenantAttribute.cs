using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Api.Services;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Domain.Tenancy;

namespace TSTHRMS.Api.Filters;

/// <summary>
/// Resolves the tenant for an anonymous career-site request from a {tenantSlug} route value,
/// since ITenantContext's only other resolution path (the JWT tenant_id claim) doesn't exist for
/// an unauthenticated visitor. Stashes the result into HttpContext.Items before the action runs,
/// so it's in place before any tenant-scoped query filter or SaveChanges auto-stamp fires.
/// </summary>
public class ResolvePublicTenantAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var slug = context.RouteData.Values["tenantSlug"] as string;
        if (string.IsNullOrWhiteSpace(slug))
        {
            context.Result = new BadRequestResult();
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug && t.Status == TenantStatus.Active);

        if (tenant is null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        context.HttpContext.Items[TenantContext.PublicTenantItemsKey] = tenant.Id;
        await next();
    }
}
