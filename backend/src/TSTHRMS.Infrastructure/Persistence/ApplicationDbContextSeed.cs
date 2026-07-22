using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TSTHRMS.Application.Common;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Identity;

namespace TSTHRMS.Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        if (await context.Tenants.AnyAsync())
        {
            return;
        }

        var tenant = new Tenant { Name = "ThinkerSteps Group", Slug = "thinkersteps" };
        context.Tenants.Add(tenant);

        context.LegalEntities.AddRange(
            new LegalEntity { TenantId = tenant.Id, Name = "The Thiinker" },
            new LegalEntity { TenantId = tenant.Id, Name = "ThinkerSteps" });

        context.Products.AddRange(
            new Product { TenantId = tenant.Id, Name = "SwarnApp" },
            new Product { TenantId = tenant.Id, Name = "JewelSteps" },
            new Product { TenantId = tenant.Id, Name = "Miniz" });

        await context.SaveChangesAsync();

        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("SeedAdmin:Email / SeedAdmin:Password not configured - skipping admin user seed");
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            TenantId = tenant.Id,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, RoleNames.HRAdmin);
        }
        else
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
    }
}
