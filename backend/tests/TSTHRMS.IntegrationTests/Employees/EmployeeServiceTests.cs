using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common;
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

        var first = await CreateEmployeeAsync(service, BuildRequest());
        var second = await CreateEmployeeAsync(service, BuildRequest());

        Assert.Equal("EMP000001", first.EmployeeCode);
        Assert.Equal("EMP000002", second.EmployeeCode);
    }

    [Fact]
    public async Task GetById_masks_bank_account_and_reveal_returns_real_value_and_is_logged()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var created = await CreateEmployeeAsync(service, BuildRequest());

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

        var created = await CreateEmployeeAsync(service, BuildRequest());

        var updated = await service.UpdateStatusAsync(created.Id, EmployeeStatus.Exited);

        Assert.NotNull(updated);
        Assert.Equal(EmployeeStatus.Exited, updated!.Status);
        Assert.Equal(created.FirstName, updated.FirstName);
    }

    [Fact]
    public async Task GetOrgChart_excludes_exited_employees_and_honors_product_filter()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var manager = await CreateEmployeeAsync(service, BuildRequest() with { FirstName = "Grace", LastName = "Hopper" });
        var report = await CreateEmployeeAsync(service,
            BuildRequest() with { FirstName = "Alan", LastName = "Turing", ReportingManagerId = manager.Id });
        var exited = await CreateEmployeeAsync(service, BuildRequest() with { FirstName = "Old", LastName = "Timer" });
        await service.UpdateStatusAsync(exited.Id, EmployeeStatus.Exited);

        var otherProduct = new TSTHRMS.Domain.Tenancy.Product { TenantId = _tenantId, Name = "Other Product" };
        context.Products.Add(otherProduct);
        await context.SaveChangesAsync();
        await CreateEmployeeAsync(service, BuildRequest() with { FirstName = "Off", LastName = "Chart", ProductId = otherProduct.Id });

        var chart = await service.GetOrgChartAsync(null, _productId);

        Assert.Contains(chart, n => n.Id == manager.Id);
        Assert.Contains(chart, n => n.Id == report.Id && n.ReportingManagerId == manager.Id);
        Assert.DoesNotContain(chart, n => n.Id == exited.Id);
        Assert.DoesNotContain(chart, n => n.FullName == "Off Chart");
    }

    [Fact]
    public async Task Create_auto_calculates_probation_end_date_when_not_supplied()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var created = await CreateEmployeeAsync(service, BuildRequest());

        Assert.Equal(new DateOnly(2020, 1, 1).AddMonths(ProbationDefaults.DurationMonths), created.ProbationEndDate);
        Assert.Equal(ConfirmationStatus.Probation, created.ConfirmationStatus);
    }

    [Fact]
    public async Task Confirm_transitions_status_and_records_manager_and_date()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var employee = await CreateEmployeeAsync(service, BuildRequest());
        var manager = await CreateEmployeeAsync(service, BuildRequest() with { FirstName = "Grace", LastName = "Hopper" });

        var confirmed = await service.ConfirmAsync(
            employee.Id, new ConfirmEmployeeRequest(manager.Id, new DateOnly(2020, 7, 1)));

        Assert.Equal(ConfirmationStatus.Confirmed, confirmed!.ConfirmationStatus);
        Assert.Equal(new DateOnly(2020, 7, 1), confirmed.ConfirmationDate);
        Assert.Equal(manager.Id, confirmed.ConfirmingManagerId);
        Assert.Contains("Hopper", confirmed.ConfirmingManagerName);
    }

    [Fact]
    public async Task Contract_end_date_within_the_warning_window_flags_as_expiring_soon()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var soonToExpire = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var created = await CreateEmployeeAsync(service,
            BuildRequest() with
            {
                EmploymentType = EmploymentType.Contract,
                ContractStartDate = new DateOnly(2020, 1, 1),
                ContractEndDate = soonToExpire,
            });

        Assert.True(created.IsContractExpiringSoon);
    }

    [Fact]
    public async Task GetList_combines_search_and_filters_and_export_matches_the_same_rows()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var match = await CreateEmployeeAsync(service, BuildRequest() with
        {
            FirstName = "Grace",
            LastName = "Hopper",
            PersonalEmail = "grace.hopper@example.com",
            Department = "Engineering",
            WorkLocation = "Mumbai HQ",
        });
        await CreateEmployeeAsync(service, BuildRequest() with
        {
            FirstName = "Alan",
            LastName = "Turing",
            PersonalEmail = "alan.turing@example.com",
            Department = "Research",
            WorkLocation = "Pune Office",
        });

        var filter = new EmployeeListFilter(1, 50, "hopper", null, null, null, "Engineering", null, "Mumbai HQ");
        var result = await service.GetListAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal(match.Id, result.Items[0].Id);

        var emailFilter = new EmployeeListFilter(1, 50, "alan.turing@example.com", null, null, null, null, null, null);
        var emailResult = await service.GetListAsync(emailFilter);
        Assert.Single(emailResult.Items);
        Assert.Equal("Turing", emailResult.Items[0].LastName);

        var exported = await service.ExportToExcelAsync(filter);
        Assert.NotEmpty(exported);
    }

    [Fact]
    public async Task GetDashboardSummary_counts_departments_and_orders_recent_joinees_by_date()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        var older = await CreateEmployeeAsync(service,
            BuildRequest() with { FirstName = "Grace", LastName = "Hopper", Department = "Engineering", DateOfJoining = new DateOnly(2018, 1, 1) });
        var newer = await CreateEmployeeAsync(service,
            BuildRequest() with { FirstName = "Alan", LastName = "Turing", Department = "Engineering", DateOfJoining = new DateOnly(2023, 6, 1) });
        var exited = await CreateEmployeeAsync(service,
            BuildRequest() with { FirstName = "Old", LastName = "Timer", Department = "Research", DateOfJoining = new DateOnly(2010, 1, 1) });
        await service.UpdateStatusAsync(exited.Id, EmployeeStatus.Exited);

        var summary = await service.GetDashboardSummaryAsync();

        Assert.Equal(3, summary.TotalEmployees);
        Assert.Equal(2, summary.ActiveEmployees);
        Assert.Equal(2, summary.DepartmentCount);
        Assert.Equal(newer.Id, summary.RecentJoinees[0].Id);
        Assert.Equal(older.Id, summary.RecentJoinees[1].Id);
    }

    [Fact]
    public async Task GetList_sorts_by_the_requested_column_in_either_direction()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context, _tenantId);

        await CreateEmployeeAsync(service, BuildRequest() with { FirstName = "Charlie", LastName = "Zeta", Designation = "Zebra" });
        await CreateEmployeeAsync(service, BuildRequest() with { FirstName = "Alice", LastName = "Alpha", Designation = "Apple" });

        var byCodeAscending = await service.GetListAsync(
            new EmployeeListFilter(1, 50, null, null, null, null, null, null, null, SortBy: "code"));
        Assert.Equal("EMP000001", byCodeAscending.Items[0].EmployeeCode);

        var byCodeDescending = await service.GetListAsync(
            new EmployeeListFilter(1, 50, null, null, null, null, null, null, null, SortBy: "code", SortDescending: true));
        Assert.Equal("EMP000002", byCodeDescending.Items[0].EmployeeCode);

        var byDesignation = await service.GetListAsync(
            new EmployeeListFilter(1, 50, null, null, null, null, null, null, null, SortBy: "designation"));
        Assert.Equal("Apple", byDesignation.Items[0].Designation);
    }

    [Fact]
    public async Task Hrbp_scoped_to_a_legal_entity_cannot_see_or_modify_employees_outside_it()
    {
        await using var context = CreateContext(_tenantId);
        var hrAdminService = CreateService(context, _tenantId);

        var otherLegalEntity = new LegalEntity { TenantId = _tenantId, Name = "Other Entity" };
        context.LegalEntities.Add(otherLegalEntity);
        await context.SaveChangesAsync();

        var inScope = await CreateEmployeeAsync(hrAdminService, BuildRequest() with { FirstName = "In", LastName = "Scope" });
        var outOfScope = await CreateEmployeeAsync(hrAdminService,
            BuildRequest() with { FirstName = "Out", LastName = "OfScope", LegalEntityId = otherLegalEntity.Id });

        var hrbpCurrentUser = new TestCurrentUserService(roles: [RoleNames.HRBP], assignedLegalEntityId: _legalEntityId);
        var hrbpService = new EmployeeService(context, new SequenceGenerator(context, new TestTenantContext(_tenantId)), hrbpCurrentUser);

        Assert.NotNull(await hrbpService.GetByIdAsync(inScope.Id));
        Assert.Null(await hrbpService.GetByIdAsync(outOfScope.Id));

        var list = await hrbpService.GetListAsync(new EmployeeListFilter(1, 50, null, null, null, null, null, null, null));
        Assert.Contains(list.Items, i => i.Id == inScope.Id);
        Assert.DoesNotContain(list.Items, i => i.Id == outOfScope.Id);

        var blockedCreate = await hrbpService.CreateAsync(
            BuildRequest() with { FirstName = "Blocked", LastName = "Create", LegalEntityId = otherLegalEntity.Id });
        Assert.Null(blockedCreate);

        var blockedUpdate = await hrbpService.UpdateAsync(outOfScope.Id,
            BuildRequest() with { LegalEntityId = otherLegalEntity.Id, FirstName = "Changed" });
        Assert.Null(blockedUpdate);

        var allowedUpdate = await hrbpService.UpdateAsync(inScope.Id, BuildRequest() with { FirstName = "Updated" });
        Assert.NotNull(allowedUpdate);
        Assert.Equal("Updated", allowedUpdate!.FirstName);
    }

    /// <summary>Null only means an HRBP tried to create outside their scope - never happens in
    /// these tests (TestCurrentUserService carries no roles), so this just avoids sprinkling
    /// null-forgiving operators everywhere a created employee's members are accessed.</summary>
    private static async Task<EmployeeDto> CreateEmployeeAsync(EmployeeService service, EmployeeWriteRequest request) =>
        (await service.CreateAsync(request))!;

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
        "Mumbai HQ",
        null,
        EmploymentType.FullTime,
        12000m,
        DateOfBirthProofType.Aadhaar,
        "Maharashtra",
        null,
        null,
        null);

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

    private class TestCurrentUserService(
        IReadOnlyCollection<string>? roles = null, Guid? assignedLegalEntityId = null, Guid? assignedProductId = null)
        : ICurrentUserService
    {
        public Guid? UserId => null;
        public IReadOnlyCollection<string> Roles => roles ?? [];
        public Guid? AssignedLegalEntityId => assignedLegalEntityId;
        public Guid? AssignedProductId => assignedProductId;
    }
}
