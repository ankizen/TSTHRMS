using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Domain.CustomFields;

public class EmployeeCustomFieldValue : TenantScopedEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid CustomFieldDefinitionId { get; set; }
    public CustomFieldDefinition? CustomFieldDefinition { get; set; }

    public string? Value { get; set; }
}
