using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Append-only decision log for a requisition (Build Notes: approvals/rejections are never
/// edited after the fact, for audit fairness). Slice 1 uses a single gate (HRAdmin/HRBP); the
/// PDF's full HRBP -> Entity Head/Finance -> Final routing is a later slice, at which point this
/// same table just accumulates more rows per requisition instead of changing shape.
/// </summary>
public class RequisitionApproval : TenantScopedEntity
{
    public Guid JobRequisitionId { get; set; }
    public JobRequisition? JobRequisition { get; set; }

    public Guid ApproverUserId { get; set; }
    public RequisitionApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}

public enum RequisitionApprovalDecision
{
    Approved,
    Rejected
}
