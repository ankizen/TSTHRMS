using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.CustomFields;

/// <summary>
/// Core HR Section 15: lets HR add new employee fields without a code change - Name is the
/// stable machine key EmployeeCustomFieldValue rows point at, Label is what's shown in the UI.
/// Value storage is a plain string regardless of FieldType (same approach as
/// EmployeeEditRequest.NewValue) - FieldType only drives how the frontend renders/validates the
/// input, not how it's persisted.
/// </summary>
public class CustomFieldDefinition : TenantScopedEntity
{
    public required string Name { get; set; }
    public required string Label { get; set; }
    public CustomFieldType FieldType { get; set; }

    /// <summary>JSON string array - only meaningful when FieldType is Select.</summary>
    public string? OptionsJson { get; set; }

    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}

public enum CustomFieldType
{
    Text,
    Number,
    Date,
    Boolean,
    Select
}
