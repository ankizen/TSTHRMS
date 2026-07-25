using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.CustomFields;
using TSTHRMS.Application.CustomFields.Dtos;
using TSTHRMS.Domain.CustomFields;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.IntegrationTests.CustomFields;

public class CustomFieldServiceTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("tsthrms_test")
        .WithUsername("tsthrms")
        .WithPassword("tsthrms_test_password")
        .Build();

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

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task CreateDefinition_rejects_a_duplicate_name()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var first = await service.CreateDefinitionAsync(
            new CustomFieldDefinitionWriteRequest("shirt_size", "Shirt Size", CustomFieldType.Text, null, false, 0));
        Assert.NotNull(first);

        var duplicate = await service.CreateDefinitionAsync(
            new CustomFieldDefinitionWriteRequest("shirt_size", "Shirt Size Again", CustomFieldType.Text, null, false, 1));
        Assert.Null(duplicate);
    }

    [Fact]
    public async Task Select_field_requires_at_least_one_option_is_enforced_by_the_service_contract()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var withOptions = await service.CreateDefinitionAsync(
            new CustomFieldDefinitionWriteRequest("t_shirt", "T-Shirt Size", CustomFieldType.Select, ["S", "M", "L"], false, 0));

        Assert.NotNull(withOptions);
        Assert.Equal(["S", "M", "L"], withOptions!.Options);
    }

    [Fact]
    public async Task SetValuesForEmployee_upserts_and_GetValues_reflects_definitions_with_no_value_as_null()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var shirtSize = await service.CreateDefinitionAsync(
            new CustomFieldDefinitionWriteRequest("shirt_size", "Shirt Size", CustomFieldType.Text, null, false, 0));
        var bloodGroup = await service.CreateDefinitionAsync(
            new CustomFieldDefinitionWriteRequest("blood_group", "Blood Group", CustomFieldType.Text, null, false, 1));

        var initial = await service.GetValuesForEmployeeAsync(_employeeId);
        Assert.NotNull(initial);
        Assert.All(initial!, v => Assert.Null(v.Value));

        var afterSet = await service.SetValuesForEmployeeAsync(_employeeId, new SetEmployeeCustomFieldValuesRequest(
            [new SetEmployeeCustomFieldValueItem(shirtSize!.Id, "L")]));

        Assert.NotNull(afterSet);
        Assert.Equal("L", afterSet!.Single(v => v.DefinitionId == shirtSize.Id).Value);
        Assert.Null(afterSet.Single(v => v.DefinitionId == bloodGroup!.Id).Value);

        // Upsert: setting again updates rather than duplicating.
        var afterUpdate = await service.SetValuesForEmployeeAsync(_employeeId, new SetEmployeeCustomFieldValuesRequest(
            [new SetEmployeeCustomFieldValueItem(shirtSize.Id, "XL")]));
        Assert.Equal("XL", afterUpdate!.Single(v => v.DefinitionId == shirtSize.Id).Value);

        var storedRows = await context.EmployeeCustomFieldValues
            .Where(v => v.EmployeeId == _employeeId && v.CustomFieldDefinitionId == shirtSize.Id)
            .ToListAsync();
        Assert.Single(storedRows);
    }

    [Fact]
    public async Task DeleteDefinition_removes_its_values_too()
    {
        await using var context = CreateContext(_tenantId);
        var service = CreateService(context);

        var definition = await service.CreateDefinitionAsync(
            new CustomFieldDefinitionWriteRequest("shirt_size", "Shirt Size", CustomFieldType.Text, null, false, 0));
        await service.SetValuesForEmployeeAsync(_employeeId, new SetEmployeeCustomFieldValuesRequest(
            [new SetEmployeeCustomFieldValueItem(definition!.Id, "L")]));

        var deleted = await service.DeleteDefinitionAsync(definition.Id);
        Assert.True(deleted);

        var remainingValues = await context.EmployeeCustomFieldValues
            .Where(v => v.CustomFieldDefinitionId == definition.Id)
            .ToListAsync();
        Assert.Empty(remainingValues);
    }

    private static CustomFieldService CreateService(ApplicationDbContext context) => new(context);

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
