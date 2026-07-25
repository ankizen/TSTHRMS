using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 2: a hiring manager's request to open a role. Must clear the approval gate
/// (<see cref="RequisitionApproval"/>) before it can be published to the Career Site as a
/// <see cref="JobPosting"/> - this is what stops jobs going live before budget/headcount is
/// actually confirmed.
/// </summary>
public class JobRequisition : TenantScopedEntity
{
    /// <summary>Auto-generated via ISequenceGenerator, e.g. "REQ000001".</summary>
    public required string RequisitionCode { get; set; }
    public required string Title { get; set; }

    public Guid LegalEntityId { get; set; }
    public LegalEntity? LegalEntity { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string? Grade { get; set; }
    public string? Department { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public int Openings { get; set; } = 1;
    public decimal? BudgetPerOpening { get; set; }
    public RequisitionReason Reason { get; set; }
    public string? JustificationNotes { get; set; }

    /// <summary>Section 5: "don't hardcode a fixed count" - carried onto the JobPosting once
    /// published so the pipeline (a later slice) knows how many interview rounds to render.</summary>
    public int InterviewRoundCount { get; set; } = 2;

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;

    /// <summary>The raising hiring manager - an ApplicationUser id, scoping visibility the same
    /// way HRBP's AssignedLegalEntityId/AssignedProductId scope Employee visibility.</summary>
    public Guid RaisedByUserId { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public ICollection<RequisitionApproval> Approvals { get; set; } = new List<RequisitionApproval>();
    public JobPosting? JobPosting { get; set; }
}

/// <summary>Backfill (replacing someone) vs a genuinely new headcount - shown on the requisition
/// so approvers know which budget bucket it draws from.</summary>
public enum RequisitionReason
{
    Backfill,
    NewRole
}

public enum RequisitionStatus
{
    Draft,
    PendingApproval,
    Approved,
    Rejected,
    OnHold,
    Closed
}
