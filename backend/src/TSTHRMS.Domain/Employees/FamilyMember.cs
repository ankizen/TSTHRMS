using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Employees;

/// <summary>
/// Core HR Section 4: repeatable family member list. Feeds ESIC dependent rules, group
/// medical/insurance enrollment, and gratuity nomination - not just record-keeping. Nominee
/// details (Section 6, a later slice) will link back to a FamilyMember by id rather than
/// duplicating name/relation, but that link is added when Section 6 is built, not here.
/// </summary>
public class FamilyMember : TenantScopedEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public FamilyRelation Relation { get; set; }
    public required string Name { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsDependent { get; set; }
    public bool IsDifferentlyAbled { get; set; }
}

public enum FamilyRelation
{
    Spouse,
    Parent,
    Child,
    Other
}
