using TSTHRMS.Domain.CustomFields;

namespace TSTHRMS.Application.CustomFields.Dtos;

public record CustomFieldDefinitionDto(
    Guid Id,
    string Name,
    string Label,
    CustomFieldType FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int DisplayOrder);

public record CustomFieldDefinitionWriteRequest(
    string Name,
    string Label,
    CustomFieldType FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int DisplayOrder);

public record EmployeeCustomFieldValueDto(
    Guid DefinitionId,
    string Name,
    string Label,
    CustomFieldType FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    string? Value);

public record SetEmployeeCustomFieldValueItem(Guid DefinitionId, string? Value);

public record SetEmployeeCustomFieldValuesRequest(IReadOnlyList<SetEmployeeCustomFieldValueItem> Values);
