using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Tenancy;

/// <summary>
/// A legal entity within a tenant (e.g. The Thiinker, ThinkerSteps). Every employee belongs to exactly one.
/// </summary>
public class LegalEntity : TenantScopedEntity
{
    public required string Name { get; set; }
    public string? Code { get; set; }

    /// <summary>Whether this entity is registered under the EPF Act - drives whether PF
    /// applicability can ever be true for its employees, independent of individual salary.</summary>
    public bool IsPfRegistered { get; set; } = true;

    /// <summary>Whether this entity is registered under the ESI Act - same role as
    /// IsPfRegistered but for ESIC.</summary>
    public bool IsEsicRegistered { get; set; } = true;
}
