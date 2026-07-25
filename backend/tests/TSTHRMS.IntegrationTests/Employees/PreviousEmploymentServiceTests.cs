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

public class PreviousEmploymentServiceTests : IAsyncLifetime
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
    public async Task Both_document_slots_attach_independently_without_clobbering_each_other()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var record = await service.CreateAsync(_employeeId, BuildRequest());

        using var relievingLetter = new MemoryStream("%PDF-1.4 relieving"u8.ToArray());
        var afterRelievingLetter = await service.AttachDocumentAsync(
            _employeeId, record!.Id, PreviousEmploymentDocumentSlot.RelievingLetter,
            relievingLetter, "relieving.pdf", "application/pdf", relievingLetter.Length);

        Assert.True(afterRelievingLetter!.Succeeded);
        Assert.Equal("relieving.pdf", afterRelievingLetter.Record!.RelievingLetterFileName);
        Assert.Null(afterRelievingLetter.Record.LastSalarySlipFileName);

        using var salarySlip = new MemoryStream("%PDF-1.4 payslip"u8.ToArray());
        var afterSalarySlip = await service.AttachDocumentAsync(
            _employeeId, record.Id, PreviousEmploymentDocumentSlot.LastSalarySlip,
            salarySlip, "payslip.pdf", "application/pdf", salarySlip.Length);

        Assert.True(afterSalarySlip!.Succeeded);
        Assert.Equal("relieving.pdf", afterSalarySlip.Record!.RelievingLetterFileName);
        Assert.Equal("payslip.pdf", afterSalarySlip.Record.LastSalarySlipFileName);
    }

    [Fact]
    public async Task GetForEmployee_orders_most_recent_previous_employer_first()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        await service.CreateAsync(_employeeId, BuildRequest("Older Corp", new DateOnly(2010, 1, 1), new DateOnly(2014, 12, 31)));
        await service.CreateAsync(_employeeId, BuildRequest("Newer Corp", new DateOnly(2016, 1, 1), new DateOnly(2019, 12, 31)));

        var records = await service.GetForEmployeeAsync(_employeeId);

        Assert.Equal("Newer Corp", records[0].CompanyName);
        Assert.Equal("Older Corp", records[1].CompanyName);
    }

    private PreviousEmploymentRecordWriteRequest BuildRequest() =>
        BuildRequest("Acme Corp", new DateOnly(2016, 1, 1), new DateOnly(2019, 12, 31));

    private PreviousEmploymentRecordWriteRequest BuildRequest(string companyName, DateOnly joining, DateOnly leaving) => new(
        companyName, "Senior Engineer", 3.5m, joining, leaving, "Better opportunity", "123456789012");

    private PreviousEmploymentService CreateService(ApplicationDbContext context) =>
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
