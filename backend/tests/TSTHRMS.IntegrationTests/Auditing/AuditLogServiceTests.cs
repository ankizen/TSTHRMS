using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Auditing;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;
using TSTHRMS.Infrastructure.Persistence.Interceptors;

namespace TSTHRMS.IntegrationTests.Auditing;

public class AuditLogServiceTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("tsthrms_test")
        .WithUsername("tsthrms")
        .WithPassword("tsthrms_test_password")
        .Build();

    private static readonly Guid ActorUserId = Guid.NewGuid();

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

        await using var seedContext = CreateContext(_tenantId);
        seedContext.LegalEntities.Add(legalEntity);
        seedContext.Products.Add(product);
        await seedContext.SaveChangesAsync();

        _legalEntityId = legalEntity.Id;
        _productId = product.Id;
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task GetEmployeeHistory_captures_field_changes_and_masks_sensitive_values_by_default()
    {
        await using var context = CreateContext(_tenantId);

        var employee = new Employee
        {
            TenantId = _tenantId,
            EmployeeCode = "EMP000001",
            LegalEntityId = _legalEntityId,
            ProductId = _productId,
            FirstName = "Grace",
            LastName = "Hopper",
            BankAccountNumber = "1234567890123456",
            DateOfJoining = new DateOnly(2020, 1, 1)
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        employee.FirstName = "Grace M.";
        await context.SaveChangesAsync();

        var service = CreateAuditLogService(context);
        var history = await service.GetEmployeeHistoryAsync(employee.Id);

        Assert.NotNull(history);
        Assert.Contains(history!, h => h.Changes.Any(c => c.PropertyName == nameof(Employee.FirstName)));

        var creationEntry = history!.Single(h => h.Action == TSTHRMS.Domain.Auditing.AuditAction.Created);
        var bankChange = creationEntry.Changes.Single(c => c.PropertyName == nameof(Employee.BankAccountNumber));
        Assert.True(bankChange.IsSensitive);
        Assert.DoesNotContain("123456789012", bankChange.NewValue ?? string.Empty);
        Assert.Contains('•', bankChange.NewValue ?? string.Empty);
    }

    [Fact]
    public async Task GetEmployeeHistory_includes_child_record_changes_in_one_timeline()
    {
        await using var context = CreateContext(_tenantId);

        var employee = new Employee
        {
            TenantId = _tenantId,
            EmployeeCode = "EMP000002",
            LegalEntityId = _legalEntityId,
            ProductId = _productId,
            FirstName = "Alan",
            LastName = "Turing",
            DateOfJoining = new DateOnly(2020, 1, 1)
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.EducationRecords.Add(new EducationRecord
        {
            TenantId = _tenantId,
            EmployeeId = employee.Id,
            QualificationLevel = QualificationLevel.Graduate,
            DegreeName = "B.Sc Mathematics",
            InstituteName = "Test University",
            YearOfPassing = 2015
        });
        await context.SaveChangesAsync();

        var service = CreateAuditLogService(context);
        var history = await service.GetEmployeeHistoryAsync(employee.Id);

        Assert.NotNull(history);
        Assert.Contains(history!, h => h.EntityName == nameof(EducationRecord));
    }

    [Fact]
    public async Task RevealEntry_unmasks_the_value_logs_a_reveal_action_and_resolves_the_actor_name()
    {
        await using var context = CreateContext(_tenantId);

        var employee = new Employee
        {
            TenantId = _tenantId,
            EmployeeCode = "EMP000003",
            LegalEntityId = _legalEntityId,
            ProductId = _productId,
            FirstName = "Ada",
            LastName = "Lovelace",
            BankAccountNumber = "9999888877776666",
            DateOfJoining = new DateOnly(2020, 1, 1)
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var service = CreateAuditLogService(context);
        var history = await service.GetEmployeeHistoryAsync(employee.Id);
        var creationEntry = history!.Single(h => h.Action == TSTHRMS.Domain.Auditing.AuditAction.Created);

        var revealed = await service.RevealEntryAsync(employee.Id, creationEntry.Id);

        Assert.NotNull(revealed);
        var bankChange = revealed!.Changes.Single(c => c.PropertyName == nameof(Employee.BankAccountNumber));
        Assert.Equal("9999888877776666", bankChange.NewValue);
        Assert.Equal("Test Actor", revealed.ChangedByDisplayName);

        var revealLogs = await context.AuditLogs
            .Where(a => a.EntityId == employee.Id.ToString() && a.Action == TSTHRMS.Domain.Auditing.AuditAction.Revealed)
            .ToListAsync();
        Assert.Single(revealLogs);
    }

    private AuditLogService CreateAuditLogService(ApplicationDbContext context) =>
        new(context, new TestTenantContext(_tenantId), new TestCurrentUserService(), new TestUserDirectory());

    private ApplicationDbContext CreateContext(Guid tenantId)
    {
        var tenantContext = new TestTenantContext(tenantId);
        var currentUserService = new TestCurrentUserService();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(_mysql.GetConnectionString(), new MySqlServerVersion(new Version(8, 4, 0)));
        optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor(tenantContext, currentUserService));
        return new ApplicationDbContext(optionsBuilder.Options, tenantContext, currentUserService);
    }

    private class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public bool IsResolved => tenantId != Guid.Empty;
    }

    private class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => ActorUserId;
    }

    private class TestUserDirectory : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, string> result = userIds.Contains(ActorUserId)
                ? new Dictionary<Guid, string> { [ActorUserId] = "Test Actor" }
                : new Dictionary<Guid, string>();
            return Task.FromResult(result);
        }
    }
}
