using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 8: one offer negotiation per application. The current terms and status-machine live
/// here; every CTC/date/designation revision is a new, immutable <see cref="OfferVersion"/> row
/// (Section 8 - "keep a version history rather than overwriting the original offer"), not an
/// update to a single set of fields.
/// </summary>
public class Offer : TenantScopedEntity
{
    public Guid ApplicationId { get; set; }
    public JobApplication? Application { get; set; }

    /// <summary>Opaque bearer token for the anonymous accept/decline link - same pattern as
    /// AssessmentSubmission.Token, since Candidate Portal login (Slice 6) doesn't exist yet.</summary>
    public required string Token { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Draft;

    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public string? DeclineReason { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    public ICollection<OfferVersion> Versions { get; set; } = new List<OfferVersion>();
}

public enum OfferStatus
{
    Draft,
    PendingApproval,
    Approved,
    Sent,
    Accepted,
    Declined,
    Expired
}
