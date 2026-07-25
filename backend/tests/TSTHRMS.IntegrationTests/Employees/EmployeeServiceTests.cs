using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Auditing;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.IntegrationTests.Employees;

public class EmployeeServiceTests : IAsyncLifetime
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

        await using (var migrateContext = CreateContext(Guid.Empty))
        {
            await migrateContext.Database.MigrateAsync();
        }

        _tenantId = Guid.NewGuid();
        var legalEntity = new LegalEntity { TenantId = _tenantId, Name = "Test Entity" };
        var product = new Product { TenantId = _tenantId, Name = "Test Product" };

        await using (var seedContext = CreateContext(_tenantId))
        {
            seedContext.LegalEntities.Add(legalEntity);
            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();
        }

        _legalEntityId = legalEntity.Id;
        _productId = product.Id;
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task Create_assigns_sequential_never_reused_employee_codes()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var first = await service.CreateAsync(BuildRequest());
        var second = await service.CreateAsync(BuildRequest());

        Assert.Equal("EMP000001", first.EmployeeCode);
        Assert.Equal("EMP000002", second.EmployeeCode);
    }

    [Fact]
    public async Task GetById_masks_bank_account_and_reveal_returns_real_value_and_is_logged()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var created = await service.CreateAsync(BuildRequest());

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.EndsWith("3456", fetched!.BankAccountNumberMasked);
        Assert.DoesNotContain("9012", fetched.BankAccountNumberMasked ?? string.Empty);
        Assert.Contains('•', fetched.BankAccountNumberMasked ?? string.Empty);

        var revealed = await service.RevealBankAccountNumberAsync(created.Id);
        Assert.Equal("1234567890123456", revealed!.BankAccountNumber);

        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString() && a.Action == AuditAction.Revealed)
            .ToListAsync();
        Assert.Single(auditLogs);
    }

    [Fact]
    public async Task UpdateStatus_transitions_status_without_touching_other_fields()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var created = await service.CreateAsync(BuildRequest());

        var updated = await service.UpdateStatusAsync(created.Id, EmployeeStatus.Exited);

        Assert.NotNull(updated);
        Assert.Equal(EmployeeStatus.Exited, updated!.Status);
        Assert.Equal(created.FirstName, updated.FirstName);
    }

    private EmployeeWriteRequest BuildRequest() => new(
        _legalEntityId,
        _productId,
        "Ada",
        "Lovelace",
        Gender.Female,
        new DateOnly(1990, 1, 1),
        "ada@example.com",
        "9999999999",
        "1 Analytical Engine Way",
        "1 Analytical Engine Way",
        "Charles Babbage",
        "Colleague",
        "8888888888",
        "1234567890123456",
        "HDFC0001234",
        new DateOnly(2020, 1, 1),
        "Engineer",
        "L3",
        "Engineering",
        null,
        EmploymentType.FullTime);

    private static EmployeeService CreateService(ApplicationDbContext context, Guid tenantId) =>
        new(context, new SequenceGenerator(context, new TestTenantContext(tenantId)), new TestCurrentUserService());

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
