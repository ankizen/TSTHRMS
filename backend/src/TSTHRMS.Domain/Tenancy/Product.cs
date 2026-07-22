using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Tenancy;

/// <summary>
/// Cost-reporting tag (e.g. SwarnApp, JewelSteps, Miniz). Independent of legal entity -
/// an employee is tagged with one of each, not a nested combination.
/// </summary>
public class Product : TenantScopedEntity
{
    public required string Name { get; set; }
}
