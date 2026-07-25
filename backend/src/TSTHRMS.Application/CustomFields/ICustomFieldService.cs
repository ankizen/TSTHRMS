using TSTHRMS.Application.CustomFields.Dtos;

namespace TSTHRMS.Application.CustomFields;

/// <summary>Section 15: lets HR add new employee fields without a code change.</summary>
public interface ICustomFieldService
{
    Task<IReadOnlyList<CustomFieldDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Null means a definition with that Name already exists for this tenant.</summary>
    Task<CustomFieldDefinitionDto?> CreateDefinitionAsync(
        CustomFieldDefinitionWriteRequest request, CancellationToken cancellationToken = default);

    Task<CustomFieldDefinitionDto?> UpdateDefinitionAsync(
        Guid id, CustomFieldDefinitionWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Every definition for the tenant, joined with this employee's stored value (null
    /// where nothing has been set yet). Null return means the employee wasn't found.</summary>
    Task<IReadOnlyList<EmployeeCustomFieldValueDto>?> GetValuesForEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeCustomFieldValueDto>?> SetValuesForEmployeeAsync(
        Guid employeeId, SetEmployeeCustomFieldValuesRequest request, CancellationToken cancellationToken = default);
}
