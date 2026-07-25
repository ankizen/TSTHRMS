using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Documents;
using TSTHRMS.Application.Documents.Dtos;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Documents;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;
using TSTHRMS.Infrastructure.Storage;

namespace TSTHRMS.IntegrationTests.Documents;

public class DocumentRepositoryServiceTests : IAsyncLifetime
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
    public async Task Upload_appears_in_the_list_and_delete_removes_it()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        using var content = new MemoryStream("%PDF-1.4 offer"u8.ToArray());
        var uploaded = await service.UploadAsync(
            _employeeId, new EmployeeDocumentWriteRequest(EmployeeDocumentCategory.OfferLetter, "Signed copy"),
            content, "offer.pdf", "application/pdf", content.Length);

        Assert.NotNull(uploaded);
        Assert.True(uploaded!.Succeeded);

        var listed = await service.GetForEmployeeAsync(_employeeId);
        Assert.Contains(listed, d => d.FileName == "offer.pdf" && d.Category == "Offer Letter");

        var deleted = await service.DeleteAsync(_employeeId, uploaded.Record!.StandaloneAttachmentId!.Value);
        Assert.True(deleted);

        var afterDelete = await service.GetForEmployeeAsync(_employeeId);
        Assert.DoesNotContain(afterDelete, d => d.FileName == "offer.pdf");
    }

    [Fact]
    public async Task GetForEmployee_aggregates_documents_from_other_core_hr_records()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var educationService = new EducationService(context, new LocalFileStorageService(new TestOptions(_storageRoot)), new TestCurrentUserService());
        var education = await educationService.CreateAsync(
            _employeeId, new EducationRecordWriteRequest(QualificationLevel.Graduate, "B.Com", "Test University", 2015, null));
        using var certificate = new MemoryStream("%PDF-1.4 cert"u8.ToArray());
        await educationService.AttachCertificateAsync(
            _employeeId, education!.Id, certificate, "degree.pdf", "application/pdf", certificate.Length);

        var documents = await service.GetForEmployeeAsync(_employeeId);

        Assert.Contains(documents, d => d.FileName == "degree.pdf" && d.Category == "Education Certificate");
    }

    private DocumentRepositoryService CreateService(ApplicationDbContext context) =>
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
