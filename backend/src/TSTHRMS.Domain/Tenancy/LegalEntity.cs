using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Tenancy;

/// <summary>
/// A legal entity within a tenant (e.g. The Thiinker, ThinkerSteps). Every employee belongs to exactly one.
/// </summary>
public class LegalEntity : TenantScopedEntity
{
    public required string Name { get; set; }
    public string? Code { get; set; }
}
