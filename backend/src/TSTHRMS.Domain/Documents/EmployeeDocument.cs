using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Domain.Documents;

/// <summary>
/// Core HR Section 10: documents attached directly to an employee that don't belong to a more
/// specific structured record (offer letter, signed policy acknowledgement, or anything else). An
/// employee filled in through Core HR directly gets EducationRecord/IdentityDocument/
/// PreviousEmploymentRecord rows with real structured fields (slices 3/5/6) instead - those stay
/// the source of truth for candidates hired the traditional way. A candidate converted from
/// Recruitment (Section 11) only ever had a plain file collected during pre-boarding (Section
/// 10), with no degree/institution/ID-number/employer metadata behind it, so those documents land
/// here as EducationCertificate/IdentityProof/PreviousEmploymentLetter for HR to review, not as
/// fabricated structured records.
/// </summary>
public class EmployeeDocument : TenantScopedEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public EmployeeDocumentCategory Category { get; set; }
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public string? Notes { get; set; }
}

public enum EmployeeDocumentCategory
{
    OfferLetter,
    PolicyAcknowledgement,
    Other,
    EducationCertificate,
    IdentityProof,
    PreviousEmploymentLetter
}
