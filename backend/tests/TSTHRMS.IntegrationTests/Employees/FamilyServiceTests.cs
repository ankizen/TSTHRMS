using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.IntegrationTests.Employees;

public class FamilyServiceTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("tsthrms_test")
        .WithUsername("tsthrms")
        .WithPassword("tsthrms_test_password")
        .Build();

    private Guid _tenantId;
    private Guid _employeeId;

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();

        _tenantId = Guid.NewGuid();

        await using var context = CreateContext(_tenantId);
        await context.Database.MigrateAsync();

        var legalEntity = new LegalEntity { TenantId = _tenantId, Name = "Test Entity" };
        var product = new Product { TenantId = _tenantId, Name = "Test Product" };
        context.LegalEntities.Add(legalEntity);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var employee = new Employee
        {
            TenantId = _tenantId,
            EmployeeCode = "EMP000001",
            LegalEntityId = legalEntity.Id,
            ProductId = product.Id,
            FirstName = "Grace",
            LastName = "Hopper",
            DateOfJoining = new DateOnly(2020, 1, 1)
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        _employeeId = employee.Id;
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task Create_update_delete_round_trip_for_a_family_member()
    {
        await using var context = CreateContext(_tenantId);
        var service = new FamilyService(context);

        var created = await service.CreateAsync(
            _employeeId,
            new FamilyMemberWriteRequest(FamilyRelation.Child, "Alan", new DateOnly(2015, 6, 1), true, false));

        Assert.NotNull(created);
        Assert.True(created!.IsDependent);
        Assert.False(created.IsDifferentlyAbled);

        var updated = await service.UpdateAsync(
            _employeeId,
            created.Id,
            new FamilyMemberWriteRequest(FamilyRelation.Child, "Alan", new DateOnly(2015, 6, 1), true, true));

        Assert.True(updated!.IsDifferentlyAbled);

        var deleted = await service.DeleteAsync(_employeeId, created.Id);
        Assert.True(deleted);

        var remaining = await service.GetForEmployeeAsync(_employeeId);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Create_for_a_different_employee_id_returns_null()
    {
        await using var context = CreateContext(_tenantId);
        var service = new FamilyService(context);

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            new FamilyMemberWriteRequest(FamilyRelation.Spouse, "Nobody", null, true, false));

        Assert.Null(result);
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
