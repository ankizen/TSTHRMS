using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Domain.Employees;

/// <summary>
/// Core HR Section 6: identity documents as their own records (not flat fields on Employee),
/// since each has its own number, proof file, and sometimes an expiry. At most one per
/// (Employee, DocumentType) - enforced in IdentityDocumentService.CreateAsync rather than a
/// database unique index, since a soft-deleted row would otherwise still occupy that slot
/// (MySQL has no partial/filtered index to exclude IsDeleted rows from the constraint).
/// </summary>
public class IdentityDocument : TenantScopedEntity, ISoftDeletable
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public IdentityDocumentType DocumentType { get; set; }

    /// <summary>Aadhaar is masked in the UI/reports per spec; PAN/Passport/UAN/ESIC are not.
    /// Flagged [Sensitive] regardless so the audit log treats every identity number
    /// conservatively.</summary>
    [Sensitive]
    public required string Number { get; set; }

    /// <summary>Only meaningful for Passport.</summary>
    public DateOnly? ExpiryDate { get; set; }

    public Guid? ProofDocumentId { get; set; }
    public Document? ProofDocument { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public enum IdentityDocumentType
{
    Pan,
    Aadhaar,
    Passport,
    Uan,
    Esic
}
