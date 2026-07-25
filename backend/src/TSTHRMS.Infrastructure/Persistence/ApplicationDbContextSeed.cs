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

        // Tenant seeding and admin-user seeding are deliberately independent idempotency checks,
        // not one "if a tenant exists, the whole thing must have already succeeded" guard - the
        // tenant is saved before the admin user is ever attempted, so if CreateAsync then fails
        // (e.g. SeedAdmin:Password doesn't meet the password policy), a single combined guard
        // would see the tenant already exists on every future restart and skip retrying the
        // admin user forever, with no way to recover except editing the database by hand.
        var tenant = await context.Tenants.FirstOrDefaultAsync();
        if (tenant is null)
        {
            tenant = new Tenant { Name = "ThinkerSteps Group", Slug = "thinkersteps" };
            context.Tenants.Add(tenant);

            context.LegalEntities.AddRange(
                new LegalEntity { TenantId = tenant.Id, Name = "The Thiinker" },
                new LegalEntity { TenantId = tenant.Id, Name = "ThinkerSteps" });

            context.Products.AddRange(
                new Product { TenantId = tenant.Id, Name = "SwarnApp" },
                new Product { TenantId = tenant.Id, Name = "JewelSteps" },
                new Product { TenantId = tenant.Id, Name = "Miniz" });

            await context.SaveChangesAsync();
        }

        if (await userManager.GetUsersInRoleAsync(RoleNames.HRAdmin) is { Count: > 0 })
        {
            return;
        }

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
