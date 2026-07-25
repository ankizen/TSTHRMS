using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Domain.Employees;

/// <summary>
/// Core HR Section 3: repeatable one-to-many qualification list - an employee can have more
/// than one (graduation + post-graduation etc.), so this is its own table, not flat columns
/// on Employee.
/// </summary>
public class EducationRecord : TenantScopedEntity, ISoftDeletable
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public QualificationLevel QualificationLevel { get; set; }
    public required string DegreeName { get; set; }
    public required string InstituteName { get; set; }
    public int YearOfPassing { get; set; }
    public string? Specialization { get; set; }

    public Guid? CertificateDocumentId { get; set; }
    public Document? CertificateDocument { get; set; }

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>Ordered lowest to highest so "highest first" sort is just an enum-value ORDER BY DESC.</summary>
public enum QualificationLevel
{
    TenthOrBelow,
    TwelfthOrDiploma,
    Graduate,
    PostGraduate,
    Doctorate,
    Other
}

public enum VerificationStatus
{
    Pending,
    Verified
}
