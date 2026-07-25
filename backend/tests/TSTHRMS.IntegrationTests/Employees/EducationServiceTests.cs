using System.Text;
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

public class EducationServiceTests : IAsyncLifetime
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

        await using (var context = CreateContext(_tenantId))
        {
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
    public async Task GetForEmployee_orders_highest_qualification_first()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        await service.CreateAsync(_employeeId, BuildRequest(QualificationLevel.Graduate));
        await service.CreateAsync(_employeeId, BuildRequest(QualificationLevel.PostGraduate));

        var records = await service.GetForEmployeeAsync(_employeeId);

        Assert.Equal(QualificationLevel.PostGraduate, records[0].QualificationLevel);
        Assert.Equal(QualificationLevel.Graduate, records[1].QualificationLevel);
    }

    [Fact]
    public async Task AttachCertificate_accepts_pdf_and_rejects_disallowed_type()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var record = await service.CreateAsync(_employeeId, BuildRequest(QualificationLevel.Graduate));

        using var pdfBytes = new MemoryStream("%PDF-1.4 fake"u8.ToArray());
        var success = await service.AttachCertificateAsync(
            _employeeId, record!.Id, pdfBytes, "degree.pdf", "application/pdf", pdfBytes.Length);

        Assert.NotNull(success);
        Assert.True(success!.Succeeded);
        Assert.Equal("degree.pdf", success.Record!.CertificateFileName);

        using var textBytes = new MemoryStream(Encoding.UTF8.GetBytes("not a real document"));
        var rejected = await service.AttachCertificateAsync(
            _employeeId, record.Id, textBytes, "notes.txt", "text/plain", textBytes.Length);

        Assert.NotNull(rejected);
        Assert.False(rejected!.Succeeded);
    }

    [Fact]
    public async Task UpdateVerificationStatus_and_delete_work_as_expected()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var record = await service.CreateAsync(_employeeId, BuildRequest(QualificationLevel.Graduate));

        var verified = await service.UpdateVerificationStatusAsync(_employeeId, record!.Id, VerificationStatus.Verified);
        Assert.Equal(VerificationStatus.Verified, verified!.VerificationStatus);

        var deleted = await service.DeleteAsync(_employeeId, record.Id);
        Assert.True(deleted);

        var remaining = await service.GetForEmployeeAsync(_employeeId);
        Assert.Empty(remaining);
    }

    private static EducationRecordWriteRequest BuildRequest(QualificationLevel level) => new(
        level, "B.Com", "Test University", 2015, "Finance");

    private EducationService CreateService(ApplicationDbContext context) =>
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
