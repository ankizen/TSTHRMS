using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Application.Employees.Validators;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.IntegrationTests.Employees;

public class EmployeeBulkImportServiceTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("tsthrms_test")
        .WithUsername("tsthrms")
        .WithPassword("tsthrms_test_password")
        .Build();

    private Guid _tenantId;
    private string _legalEntityName = "";
    private string _productName = "";

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

        _legalEntityName = legalEntity.Name;
        _productName = product.Name;
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task GetTemplate_produces_a_workbook_with_the_expected_header_row()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var bytes = service.GetTemplate();

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheets.First();
        Assert.Equal("Legal Entity", sheet.Cell(1, 1).GetString());
        Assert.Equal("First Name", sheet.Cell(1, 3).GetString());
        Assert.Equal("Employment Type", sheet.Cell(1, 14).GetString());
    }

    [Fact]
    public async Task ValidateAsync_reports_row_level_errors_without_creating_anything()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        using var upload = BuildWorkbook(
            ValidRow("Grace", "Hopper"),
            // Bad gender value.
            new object?[] { _legalEntityName, _productName, "Alan", "Turing", "NotAGender", null, null, null, "2024-01-01", null, null, null, null, "FullTime", null, null, null, null },
            // Missing first name.
            new object?[] { _legalEntityName, _productName, null, "NoFirstName", "Male", null, null, null, "2024-01-01", null, null, null, null, "FullTime", null, null, null, null },
            // Unknown product.
            new object?[] { _legalEntityName, "Nonexistent Product", "Jane", "Doe", "Female", null, null, null, "2024-01-01", null, null, null, null, "FullTime", null, null, null, null });

        var summary = await service.ValidateAsync(upload);

        Assert.Equal(4, summary.TotalRows);
        Assert.Equal(1, summary.ValidRows);
        Assert.Equal(3, summary.InvalidRows);
        Assert.Equal(0, summary.CreatedCount);
        Assert.All(summary.Rows, r => Assert.Null(r.EmployeeCode));

        var genderRow = summary.Rows.Single(r => r.RowNumber == 3);
        Assert.Contains(genderRow.Errors, e => e.Contains("Gender"));

        var missingNameRow = summary.Rows.Single(r => r.RowNumber == 4);
        Assert.Contains(missingNameRow.Errors, e => e.Contains("First Name"));

        var unknownProductRow = summary.Rows.Single(r => r.RowNumber == 5);
        Assert.Contains(unknownProductRow.Errors, e => e.Contains("Nonexistent Product"));

        Assert.Equal(0, await context.Employees.CountAsync());
    }

    [Fact]
    public async Task CommitAsync_creates_only_the_valid_rows_and_skips_the_rest()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        using var upload = BuildWorkbook(
            ValidRow("Grace", "Hopper"),
            ValidRow("Alan", "Turing"),
            new object?[] { _legalEntityName, _productName, null, "NoFirstName", "Male", null, null, null, "2024-01-01", null, null, null, null, "FullTime", null, null, null, null });

        var summary = await service.CommitAsync(upload);

        Assert.Equal(3, summary.TotalRows);
        Assert.Equal(2, summary.ValidRows);
        Assert.Equal(2, summary.CreatedCount);

        var created = summary.Rows.Where(r => r.IsValid).ToList();
        Assert.All(created, r => Assert.NotNull(r.EmployeeCode));

        var failed = summary.Rows.Single(r => !r.IsValid);
        Assert.Null(failed.EmployeeCode);

        Assert.Equal(2, await context.Employees.CountAsync());
    }

    private object?[] ValidRow(string firstName, string lastName) =>
        [_legalEntityName, _productName, firstName, lastName, "Female", "1990-01-01",
            $"{firstName}.{lastName}@example.com".ToLowerInvariant(), "9999999999", "2024-01-01",
            "Engineer", "L2", "Engineering", "Mumbai HQ", "FullTime", "50000", "Maharashtra", null, null];

    private static MemoryStream BuildWorkbook(params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Employees");

        string[] headers =
        [
            "Legal Entity", "Product", "First Name", "Last Name", "Gender", "Date of Birth",
            "Personal Email", "Personal Phone", "Date of Joining", "Designation", "Grade",
            "Department", "Work Location", "Employment Type", "Monthly Gross Salary",
            "Professional Tax State", "Bank Account Number", "Bank IFSC Code"
        ];
        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
        }

        for (var row = 0; row < rows.Length; row++)
        {
            for (var col = 0; col < rows[row].Length; col++)
            {
                var value = rows[row][col];
                if (value is not null)
                {
                    sheet.Cell(row + 2, col + 1).Value = value.ToString();
                }
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static EmployeeBulkImportService CreateService(ApplicationDbContext context, Guid tenantId) =>
        new(context,
            new EmployeeService(context, new SequenceGenerator(context, new TestTenantContext(tenantId)), new TestCurrentUserService()),
            new EmployeeWriteRequestValidator());

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
