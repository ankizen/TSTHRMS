using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.CustomFields.Dtos;
using TSTHRMS.Domain.CustomFields;

namespace TSTHRMS.Application.CustomFields;

public class CustomFieldService(IApplicationDbContext dbContext) : ICustomFieldService
{
    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync(cancellationToken);

        return definitions.Select(ToDto).ToList();
    }

    public async Task<CustomFieldDefinitionDto?> CreateDefinitionAsync(
        CustomFieldDefinitionWriteRequest request, CancellationToken cancellationToken = default)
    {
        var nameExists = await dbContext.CustomFieldDefinitions.AnyAsync(d => d.Name == request.Name, cancellationToken);
        if (nameExists)
        {
            return null;
        }

        var definition = new CustomFieldDefinition
        {
            Name = request.Name,
            Label = request.Label,
            FieldType = request.FieldType,
            OptionsJson = SerializeOptions(request.Options),
            IsRequired = request.IsRequired,
            DisplayOrder = request.DisplayOrder
        };

        dbContext.CustomFieldDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(definition);
    }

    public async Task<CustomFieldDefinitionDto?> UpdateDefinitionAsync(
        Guid id, CustomFieldDefinitionWriteRequest request, CancellationToken cancellationToken = default)
    {
        var definition = await dbContext.CustomFieldDefinitions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (definition is null)
        {
            return null;
        }

        var nameTakenByAnother = await dbContext.CustomFieldDefinitions
            .AnyAsync(d => d.Id != id && d.Name == request.Name, cancellationToken);
        if (nameTakenByAnother)
        {
            return null;
        }

        definition.Name = request.Name;
        definition.Label = request.Label;
        definition.FieldType = request.FieldType;
        definition.OptionsJson = SerializeOptions(request.Options);
        definition.IsRequired = request.IsRequired;
        definition.DisplayOrder = request.DisplayOrder;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(definition);
    }

    public async Task<bool> DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var definition = await dbContext.CustomFieldDefinitions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (definition is null)
        {
            return false;
        }

        // Config metadata, not employee data - Section 15's soft-delete rule is about
        // Employee/Education/Family/PreviousEmployment/IdentityDocument/Nominee records, not the
        // admin-only field definitions themselves. Removing a field removes its values with it,
        // the same way dropping a column would.
        dbContext.CustomFieldDefinitions.Remove(definition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<EmployeeCustomFieldValueDto>?> GetValuesForEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            return null;
        }

        var definitions = await dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync(cancellationToken);

        var values = await dbContext.EmployeeCustomFieldValues
            .AsNoTracking()
            .Where(v => v.EmployeeId == employeeId)
            .ToDictionaryAsync(v => v.CustomFieldDefinitionId, v => v.Value, cancellationToken);

        return definitions
            .Select(d => new EmployeeCustomFieldValueDto(
                d.Id, d.Name, d.Label, d.FieldType, DeserializeOptions(d.OptionsJson), d.IsRequired,
                values.TryGetValue(d.Id, out var value) ? value : null))
            .ToList();
    }

    public async Task<IReadOnlyList<EmployeeCustomFieldValueDto>?> SetValuesForEmployeeAsync(
        Guid employeeId, SetEmployeeCustomFieldValuesRequest request, CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            return null;
        }

        var validDefinitionIds = (await dbContext.CustomFieldDefinitions
            .Select(d => d.Id)
            .ToListAsync(cancellationToken)).ToHashSet();

        var existingValues = await dbContext.EmployeeCustomFieldValues
            .Where(v => v.EmployeeId == employeeId)
            .ToDictionaryAsync(v => v.CustomFieldDefinitionId, cancellationToken);

        foreach (var item in request.Values)
        {
            // Silently ignored rather than rejected - a field deleted after the form was loaded
            // shouldn't block saving the rest of the submission.
            if (!validDefinitionIds.Contains(item.DefinitionId))
            {
                continue;
            }

            if (existingValues.TryGetValue(item.DefinitionId, out var existing))
            {
                existing.Value = item.Value;
            }
            else
            {
                dbContext.EmployeeCustomFieldValues.Add(new EmployeeCustomFieldValue
                {
                    EmployeeId = employeeId,
                    CustomFieldDefinitionId = item.DefinitionId,
                    Value = item.Value
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetValuesForEmployeeAsync(employeeId, cancellationToken);
    }

    private static CustomFieldDefinitionDto ToDto(CustomFieldDefinition definition) => new(
        definition.Id, definition.Name, definition.Label, definition.FieldType,
        DeserializeOptions(definition.OptionsJson), definition.IsRequired, definition.DisplayOrder);

    private static string? SerializeOptions(IReadOnlyList<string>? options) =>
        options is null || options.Count == 0 ? null : JsonSerializer.Serialize(options);

    private static IReadOnlyList<string>? DeserializeOptions(string? optionsJson) =>
        optionsJson is null ? null : JsonSerializer.Deserialize<List<string>>(optionsJson);
}
