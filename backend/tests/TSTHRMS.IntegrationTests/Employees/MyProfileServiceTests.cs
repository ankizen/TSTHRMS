using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.IntegrationTests.Employees;

public class MyProfileServiceTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("tsthrms_test")
        .WithUsername("tsthrms")
        .WithPassword("tsthrms_test_password")
        .Build();

    private Guid _tenantId;
    private Guid _legalEntityId;
    private Guid _productId;

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();

        await using var migrateContext = CreateContext(Guid.Empty, null);
        await migrateContext.Database.MigrateAsync();

        _tenantId = Guid.NewGuid();
        var legalEntity = new LegalEntity { TenantId = _tenantId, Name = "Test Entity" };
        var product = new Product { TenantId = _tenantId, Name = "Test Product" };

        await using var seedContext = CreateContext(_tenantId, null);
        seedContext.LegalEntities.Add(legalEntity);
        seedContext.Products.Add(product);
        await seedContext.SaveChangesAsync();

        _legalEntityId = legalEntity.Id;
        _productId = product.Id;
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task GetOwnProfile_returns_the_callers_own_record_and_null_when_unlinked()
    {
        await using var context = CreateContext(_tenantId, null);

        var manager = NewEmployee("Grace", "Hopper");
        context.Employees.Add(manager);
        await context.SaveChangesAsync();

        var linkedService = BuildService(context, _tenantId, manager.Id);
        var profile = await linkedService.GetOwnProfileAsync();
        Assert.NotNull(profile);
        Assert.Equal("Grace", profile!.FirstName);

        var unlinkedService = BuildService(context, _tenantId, null);
        Assert.Null(await unlinkedService.GetOwnProfileAsync());
    }

    [Fact]
    public async Task GetDirectReports_returns_only_employees_reporting_to_the_caller_with_restricted_fields()
    {
        await using var context = CreateContext(_tenantId, null);

        var manager = NewEmployee("Grace", "Hopper");
        context.Employees.Add(manager);
        await context.SaveChangesAsync();

        var report = NewEmployee("Alan", "Turing");
        report.ReportingManagerId = manager.Id;
        report.MonthlyGrossSalary = 999999m;
        var stranger = NewEmployee("Off", "Chart");
        context.Employees.AddRange(report, stranger);
        await context.SaveChangesAsync();

        var service = BuildService(context, _tenantId, manager.Id);
        var reports = await service.GetDirectReportsAsync();

        Assert.Single(reports);
        Assert.Equal("Turing", reports[0].LastName);

        var unlinkedService = BuildService(context, _tenantId, null);
        Assert.Empty(await unlinkedService.GetDirectReportsAsync());
    }

    private Employee NewEmployee(string firstName, string lastName) => new()
    {
        TenantId = _tenantId,
        EmployeeCode = $"EMP-{Guid.NewGuid():N}"[..12],
        LegalEntityId = _legalEntityId,
        ProductId = _productId,
        FirstName = firstName,
        LastName = lastName,
        DateOfJoining = new DateOnly(2020, 1, 1)
    };

    private MyProfileService BuildService(ApplicationDbContext context, Guid tenantId, Guid? employeeId)
    {
        var currentUserService = new TestCurrentUserService(employeeId);
        var employeeService = new EmployeeService(context, new SequenceGenerator(context, new TestTenantContext(tenantId)), currentUserService);
        return new MyProfileService(context, employeeService, currentUserService);
    }

    private ApplicationDbContext CreateContext(Guid tenantId, Guid? employeeId)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(_mysql.GetConnectionString(), new MySqlServerVersion(new Version(8, 4, 0)));
        return new ApplicationDbContext(optionsBuilder.Options, new TestTenantContext(tenantId), new TestCurrentUserService(employeeId));
    }

    private class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public bool IsResolved => tenantId != Guid.Empty;
    }

    private class TestCurrentUserService(Guid? employeeId) : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? EmployeeId => employeeId;
    }
}
