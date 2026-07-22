using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.IntegrationTests.Persistence;

/// <summary>
/// The one test that must never be allowed to go red: a query scoped to one tenant must
/// never return another tenant's rows, regardless of how many entities get added later.
/// </summary>
public class TenantIsolationTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("tsthrms_test")
        .WithUsername("tsthrms")
        .WithPassword("tsthrms_test_password")
        .Build();

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();

        await using var context = CreateContext(Guid.Empty);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task Query_scoped_to_one_tenant_never_returns_another_tenants_rows()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        await SeedLegalEntityAsync(tenantAId, "Tenant A Entity");
        await SeedLegalEntityAsync(tenantBId, "Tenant B Entity");

        await using var scopedToA = CreateContext(tenantAId);
        var visibleToA = await scopedToA.LegalEntities.ToListAsync();

        var onlyEntity = Assert.Single(visibleToA);
        Assert.Equal("Tenant A Entity", onlyEntity.Name);
        Assert.Equal(tenantAId, onlyEntity.TenantId);
    }

    [Fact]
    public async Task Adding_an_entity_auto_stamps_the_current_tenant_id()
    {
        var tenantId = Guid.NewGuid();

        await using var context = CreateContext(tenantId);
        var product = new Product { TenantId = Guid.Empty, Name = "AutoStampTest" };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        Assert.Equal(tenantId, product.TenantId);
    }

    private async Task SeedLegalEntityAsync(Guid tenantId, string name)
    {
        await using var context = CreateContext(tenantId);
        context.LegalEntities.Add(new LegalEntity { TenantId = tenantId, Name = name });
        await context.SaveChangesAsync();
    }

    private ApplicationDbContext CreateContext(Guid tenantId)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(_mysql.GetConnectionString(), new MySqlServerVersion(new Version(8, 4, 0)));
        return new ApplicationDbContext(optionsBuilder.Options, new TestTenantContext(tenantId), new TestCurrentUserService());
    }

    private class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public bool IsResolved => tenantId != Guid.Empty;
    }

    private class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
    }
}
