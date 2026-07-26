using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 13 (DPDPA 2023): a candidate's own request, from the Candidate Portal, to erase their
/// personal data ("right to erasure"). HR decides; approving anonymizes the Candidate row
/// immediately (bypassing the retention timer and, unlike the automatic sweep, even overriding
/// IsInTalentPool - an explicit erasure request always wins over HR wanting to keep them in mind).
/// </summary>
public class CandidateDataDeletionRequest : TenantScopedEntity
{
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public CandidateDataDeletionRequestStatus Status { get; set; } = CandidateDataDeletionRequestStatus.Pending;

    public string? HrDecisionNotes { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}

public enum CandidateDataDeletionRequestStatus
{
    Pending,
    Approved,
    Rejected
}
