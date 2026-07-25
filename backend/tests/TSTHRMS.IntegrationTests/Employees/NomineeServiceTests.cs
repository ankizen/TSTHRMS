using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;
using TSTHRMS.Infrastructure.Storage;

namespace TSTHRMS.IntegrationTests.Employees;

public class NomineeServiceTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("tsthrms_test")
        .WithUsername("tsthrms")
        .WithPassword("tsthrms_test_password")
        .Build();

    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), $"tsthrms-test-{Guid.NewGuid():N}");

    private Guid _tenantId;
    private Guid _employeeId;
    private Guid _otherEmployeeId;
    private Guid _familyMemberId;

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
        var otherEmployee = new Employee
        {
            TenantId = _tenantId,
            EmployeeCode = "EMP000002",
            LegalEntityId = legalEntity.Id,
            ProductId = product.Id,
            FirstName = "Ada",
            LastName = "Lovelace",
            DateOfJoining = new DateOnly(2020, 1, 1)
        };
        context.Employees.AddRange(employee, otherEmployee);
        await context.SaveChangesAsync();

        _employeeId = employee.Id;
        _otherEmployeeId = otherEmployee.Id;

        var familyMember = new FamilyMember
        {
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            Relation = FamilyRelation.Spouse,
            Name = "Vivian Hopper",
            IsDependent = true
        };
        context.FamilyMembers.Add(familyMember);
        await context.SaveChangesAsync();
        _familyMemberId = familyMember.Id;
    }

    public async Task DisposeAsync()
    {
        await _mysql.DisposeAsync();
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Two_pf_nominees_totalling_exactly_100_percent_both_succeed()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var first = await service.CreateAsync(
            _employeeId, new NomineeWriteRequest(NominationType.ProvidentFund, "Vivian Hopper", "Spouse", 60m, null, null));
        var second = await service.CreateAsync(
            _employeeId, new NomineeWriteRequest(NominationType.ProvidentFund, "Roger Hopper", "Son", 40m, null, null));

        Assert.True(first!.Succeeded);
        Assert.True(second!.Succeeded);
    }

    [Fact]
    public async Task A_pf_nominee_share_that_would_exceed_100_percent_is_rejected()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        await service.CreateAsync(
            _employeeId, new NomineeWriteRequest(NominationType.ProvidentFund, "Vivian Hopper", "Spouse", 70m, null, null));
        var second = await service.CreateAsync(
            _employeeId, new NomineeWriteRequest(NominationType.ProvidentFund, "Roger Hopper", "Son", 40m, null, null));

        Assert.False(second!.Succeeded);
        Assert.Contains("100%", second.Error);
    }

    [Fact]
    public async Task Gratuity_and_pf_shares_are_tracked_independently()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var pf = await service.CreateAsync(
            _employeeId, new NomineeWriteRequest(NominationType.ProvidentFund, "Vivian Hopper", "Spouse", 100m, null, null));
        var gratuity = await service.CreateAsync(
            _employeeId, new NomineeWriteRequest(NominationType.Gratuity, "Vivian Hopper", "Spouse", 100m, null, null));

        Assert.True(pf!.Succeeded);
        Assert.True(gratuity!.Succeeded);
    }

    [Fact]
    public async Task Linking_a_family_member_from_a_different_employee_is_rejected()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var result = await service.CreateAsync(
            _otherEmployeeId,
            new NomineeWriteRequest(NominationType.Insurance, "Vivian Hopper", "Spouse", null, "9999999999", _familyMemberId));

        Assert.False(result!.Succeeded);
        Assert.Contains("does not belong", result.Error);
    }

    [Fact]
    public async Task Linking_a_valid_family_member_populates_its_name_in_the_dto()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var result = await service.CreateAsync(
            _employeeId,
            new NomineeWriteRequest(NominationType.Insurance, "Vivian Hopper", "Spouse", null, "9999999999", _familyMemberId));

        Assert.True(result!.Succeeded);
        Assert.Equal("Vivian Hopper", result.Record!.FamilyMemberName);
    }

    private NomineeService CreateService(ApplicationDbContext context) =>
        new(context, new LocalFileStorageService(new TestOptions(_storageRoot)));

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

    private class TestOptions(string rootPath) : Microsoft.Extensions.Options.IOptions<LocalFileStorageOptions>
    {
        public LocalFileStorageOptions Value { get; } = new() { RootPath = rootPath };
    }
}
