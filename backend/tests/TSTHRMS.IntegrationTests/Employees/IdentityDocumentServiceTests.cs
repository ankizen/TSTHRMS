using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Auditing;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;
using TSTHRMS.Infrastructure.Storage;

namespace TSTHRMS.IntegrationTests.Employees;

public class IdentityDocumentServiceTests : IAsyncLifetime
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

    public async Task DisposeAsync()
    {
        await _mysql.DisposeAsync();
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Creating_a_second_document_of_the_same_type_is_rejected_as_a_conflict()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var first = await service.CreateAsync(
            _employeeId, new IdentityDocumentWriteRequest(IdentityDocumentType.Pan, "ABCDE1234F", null));
        Assert.True(first!.Succeeded);

        var second = await service.CreateAsync(
            _employeeId, new IdentityDocumentWriteRequest(IdentityDocumentType.Pan, "ZZZZZ9999Z", null));

        Assert.False(second!.Succeeded);
        Assert.Contains("already exists", second.Error);
    }

    [Fact]
    public async Task Aadhaar_is_masked_in_the_list_but_pan_is_not()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        await service.CreateAsync(_employeeId, new IdentityDocumentWriteRequest(IdentityDocumentType.Pan, "ABCDE1234F", null));
        await service.CreateAsync(_employeeId, new IdentityDocumentWriteRequest(IdentityDocumentType.Aadhaar, "123456789012", null));

        var documents = await service.GetForEmployeeAsync(_employeeId);

        var pan = documents.Single(d => d.DocumentType == IdentityDocumentType.Pan);
        var aadhaar = documents.Single(d => d.DocumentType == IdentityDocumentType.Aadhaar);

        Assert.Equal("ABCDE1234F", pan.NumberDisplay);
        Assert.Equal("••••••••9012", aadhaar.NumberDisplay);
    }

    [Fact]
    public async Task Reveal_returns_the_real_number_and_logs_it()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var created = await service.CreateAsync(
            _employeeId, new IdentityDocumentWriteRequest(IdentityDocumentType.Aadhaar, "123456789012", null));

        var revealed = await service.RevealNumberAsync(_employeeId, created!.Record!.Id);

        Assert.Equal("123456789012", revealed!.Number);

        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Record.Id.ToString() && a.Action == AuditAction.Revealed)
            .ToListAsync();
        Assert.Single(auditLogs);
    }

    [Fact]
    public async Task Delete_soft_deletes_so_a_new_document_of_the_same_type_can_be_added_afterward()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var first = await service.CreateAsync(
            _employeeId, new IdentityDocumentWriteRequest(IdentityDocumentType.Pan, "ABCDE1234F", null));

        var deleted = await service.DeleteAsync(_employeeId, first!.Record!.Id);
        Assert.True(deleted);

        var afterDelete = await service.GetForEmployeeAsync(_employeeId);
        Assert.Empty(afterDelete);

        // The old DB unique index would have blocked this - it's gone now that delete is soft.
        var second = await service.CreateAsync(
            _employeeId, new IdentityDocumentWriteRequest(IdentityDocumentType.Pan, "ZZZZZ9999Z", null));
        Assert.True(second!.Succeeded);

        var deletedRowStillExists = await context.IdentityDocuments
            .IgnoreQueryFilters()
            .AnyAsync(d => d.Id == first.Record.Id && d.IsDeleted);
        Assert.True(deletedRowStillExists);
    }

    private IdentityDocumentService CreateService(ApplicationDbContext context) =>
        new(context, new LocalFileStorageService(new TestOptions(_storageRoot)), new TestCurrentUserService());

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
