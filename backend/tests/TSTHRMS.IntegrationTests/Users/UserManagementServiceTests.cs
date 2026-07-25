using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MySql;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Users.Dtos;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Identity;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.IntegrationTests.Users;

public class UserManagementServiceTests : IAsyncLifetime
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

        await using var context = CreateContext(Guid.Empty);
        await context.Database.MigrateAsync();

        _tenantId = Guid.NewGuid();
        var legalEntity = new LegalEntity { TenantId = _tenantId, Name = "Test Entity" };
        var product = new Product { TenantId = _tenantId, Name = "Test Product" };
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

        await using var seedContext = CreateContext(_tenantId);
        seedContext.LegalEntities.Add(legalEntity);
        seedContext.Products.Add(product);
        seedContext.Employees.Add(employee);
        await seedContext.SaveChangesAsync();

        _employeeId = employee.Id;
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task CreateAsync_provisions_a_login_linked_to_the_employee_with_the_requested_role()
    {
        await using var context = CreateContext(_tenantId);
        var (service, roleManager) = await CreateServiceAsync(context, _tenantId);

        var result = await service.CreateAsync(new CreateUserRequest(
            _employeeId, "hrbp@example.com", "Sup3rSecret!23", RoleNames.HRBP, null, null));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.Equal("hrbp@example.com", result.User!.Email);
        Assert.Equal(_employeeId, result.User.EmployeeId);
        Assert.Contains(RoleNames.HRBP, result.User.Roles);
        Assert.Contains("Hopper", result.User.EmployeeName);

        var listed = await service.GetListAsync();
        Assert.Contains(listed, u => u.Email == "hrbp@example.com");

        roleManager.Dispose();
    }

    [Fact]
    public async Task CreateAsync_rejects_an_unknown_role()
    {
        await using var context = CreateContext(_tenantId);
        var (service, roleManager) = await CreateServiceAsync(context, _tenantId);

        var result = await service.CreateAsync(new CreateUserRequest(
            _employeeId, "someone@example.com", "Sup3rSecret!23", "NotARole", null, null));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("NotARole"));

        roleManager.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_removes_the_login()
    {
        await using var context = CreateContext(_tenantId);
        var (service, roleManager) = await CreateServiceAsync(context, _tenantId);

        var created = await service.CreateAsync(new CreateUserRequest(
            _employeeId, "toremove@example.com", "Sup3rSecret!23", RoleNames.Manager, null, null));

        var deleted = await service.DeleteAsync(created.User!.Id);
        Assert.True(deleted);

        var listed = await service.GetListAsync();
        Assert.DoesNotContain(listed, u => u.Email == "toremove@example.com");

        roleManager.Dispose();
    }

    private static async Task<(UserManagementService Service, RoleManager<ApplicationRole> RoleManager)> CreateServiceAsync(
        ApplicationDbContext context, Guid tenantId)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(context);
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        return (new UserManagementService(userManager, context, new TestTenantContext(tenantId)), roleManager);
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
