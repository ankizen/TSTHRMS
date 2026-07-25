using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Domain.Employees;

/// <summary>
/// Core HR Section 5: repeatable previous-employer history. Relieving letter and last salary
/// slip are needed for PF transfer and background verification; previous UAN (if different)
/// links the PF transfer-in.
/// </summary>
public class PreviousEmploymentRecord : TenantScopedEntity, ISoftDeletable
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public required string CompanyName { get; set; }
    public string? Designation { get; set; }
    public decimal? YearsOfExperience { get; set; }
    public DateOnly DateOfJoining { get; set; }
    public DateOnly DateOfLeaving { get; set; }
    public string? ReasonForLeaving { get; set; }
    public string? PreviousUan { get; set; }

    public Guid? RelievingLetterDocumentId { get; set; }
    public Document? RelievingLetterDocument { get; set; }

    public Guid? LastSalarySlipDocumentId { get; set; }
    public Document? LastSalarySlipDocument { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public enum PreviousEmploymentDocumentSlot
{
    RelievingLetter,
    LastSalarySlip
}
