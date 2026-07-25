using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Domain.Employees;

/// <summary>
/// Core HR Section 6: PF/Gratuity/Insurance nominees, kept separate from FamilyMember since
/// a nominee isn't always a family member. When one is, FamilyMemberId links back instead of
/// duplicating name/relation entry (the "don't duplicate data entry" note from Section 4).
/// </summary>
public class Nominee : TenantScopedEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public NominationType NominationType { get; set; }
    public required string Name { get; set; }
    public required string Relation { get; set; }

    /// <summary>PF/Gratuity nominee shares must total 100% across nominees of the same type;
    /// not meaningful for Insurance nominees.</summary>
    public decimal? SharePercentage { get; set; }

    public string? ContactNumber { get; set; }

    public Guid? FamilyMemberId { get; set; }
    public FamilyMember? FamilyMember { get; set; }

    /// <summary>Form 2 equivalent - signature/consent upload.</summary>
    public Guid? ConsentDocumentId { get; set; }
    public Document? ConsentDocument { get; set; }
}

public enum NominationType
{
    ProvidentFund,
    Gratuity,
    Insurance
}
