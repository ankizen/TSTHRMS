using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.IntegrationTests.Employees;

public class EmployeeEditRequestServiceTests : IAsyncLifetime
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
    private Guid _employeeId;

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

        var employee = new Employee
        {
            TenantId = _tenantId,
            EmployeeCode = "EMP000001",
            LegalEntityId = _legalEntityId,
            ProductId = _productId,
            FirstName = "Ada",
            LastName = "Lovelace",
            PersonalPhone = "1111111111",
            DateOfJoining = new DateOnly(2020, 1, 1)
        };
        await using var employeeContext = CreateContext(_tenantId, null);
        employeeContext.Employees.Add(employee);
        await employeeContext.SaveChangesAsync();
        _employeeId = employee.Id;
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task SubmitAsync_snapshots_the_old_value_and_leaves_the_record_unchanged_until_approved()
    {
        await using var context = CreateContext(_tenantId, _employeeId);
        var service = BuildService(context, _employeeId);

        var created = await service.SubmitAsync(new SubmitEditRequestsRequest(
            [new SubmitEditRequestItem(EditableEmployeeField.PersonalPhone, "2222222222")]));

        Assert.Single(created);
        Assert.Equal("1111111111", created[0].OldValue);
        Assert.Equal("2222222222", created[0].NewValue);
        Assert.Equal(EditRequestStatus.Pending, created[0].Status);

        var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == _employeeId);
        Assert.Equal("1111111111", employee.PersonalPhone);
    }

    [Fact]
    public async Task ApproveAsync_applies_the_change_and_records_the_reviewer()
    {
        await using var context = CreateContext(_tenantId, _employeeId);
        var employeeSideService = BuildService(context, _employeeId);

        var created = await employeeSideService.SubmitAsync(new SubmitEditRequestsRequest(
            [new SubmitEditRequestItem(EditableEmployeeField.PersonalPhone, "2222222222")]));

        var hrService = BuildService(context, null);
        var pending = await hrService.GetPendingAsync();
        Assert.Contains(pending, r => r.Id == created[0].Id);

        var approved = await hrService.ApproveAsync(created[0].Id, new ReviewEditRequestDto("Looks good"));

        Assert.NotNull(approved);
        Assert.Equal(EditRequestStatus.Approved, approved!.Status);
        Assert.Equal("Looks good", approved.ReviewNote);

        var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == _employeeId);
        Assert.Equal("2222222222", employee.PersonalPhone);

        var stillPending = await hrService.GetPendingAsync();
        Assert.DoesNotContain(stillPending, r => r.Id == created[0].Id);
    }

    [Fact]
    public async Task RejectAsync_leaves_the_employee_record_unchanged()
    {
        await using var context = CreateContext(_tenantId, _employeeId);
        var employeeSideService = BuildService(context, _employeeId);

        var created = await employeeSideService.SubmitAsync(new SubmitEditRequestsRequest(
            [new SubmitEditRequestItem(EditableEmployeeField.PersonalPhone, "3333333333")]));

        var hrService = BuildService(context, null);
        var rejected = await hrService.RejectAsync(created[0].Id, new ReviewEditRequestDto("Not needed"));

        Assert.NotNull(rejected);
        Assert.Equal(EditRequestStatus.Rejected, rejected!.Status);

        var employee = await context.Employees.AsNoTracking().FirstAsync(e => e.Id == _employeeId);
        Assert.Equal("1111111111", employee.PersonalPhone);
    }

    private static EmployeeEditRequestService BuildService(ApplicationDbContext context, Guid? employeeId) =>
        new(context, new TestCurrentUserService(employeeId), new TestUserDirectory());

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

    private class TestUserDirectory : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<Guid?> GetEmployeeIdForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);
    }
}
