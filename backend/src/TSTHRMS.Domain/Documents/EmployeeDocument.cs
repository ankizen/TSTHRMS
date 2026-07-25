using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Domain.Documents;

/// <summary>
/// Core HR Section 10: documents attached directly to an employee that don't belong to a more
/// specific record (offer letter, signed policy acknowledgement, or anything else). Education
/// certificates, previous-employment letters, identity proofs, and nominee consent forms are
/// already attached to their own records (slices 3/5/6) and don't need a row here - the
/// consolidated document view (DocumentRepositoryService) merges both sources for display.
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
    Other
}
