using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 6: one test attempt for one application - at most one per application (a genuine
/// retake is a new application/posting later, gated by RetakeAllowedAfter, not a second row
/// here). Delivered as an anonymous, tokenized link since the Candidate Portal login (Section 3)
/// doesn't exist yet - the token stands in for that until Slice 6 adds real candidate auth.
/// </summary>
public class AssessmentSubmission : TenantScopedEntity
{
    public Guid ApplicationId { get; set; }
    public JobApplication? Application { get; set; }

    /// <summary>Opaque bearer token for the public link - never derived from guessable data.</summary>
    public required string Token { get; set; }

    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public string? SubmissionText { get; set; }
    public Guid? SubmissionDocumentId { get; set; }
    public Document? SubmissionDocument { get; set; }

    /// <summary>Section 6: "auto-scored where possible... manual reviewer scoring... for
    /// assignments and case studies" - MVP scores everything manually (0-100); a real
    /// auto-scoring integration for coding/aptitude tests is a later hook, not built here.</summary>
    public int? Score { get; set; }

    /// <summary>Derived from Score vs the posting's AssessmentPassThreshold at scoring time, not
    /// recomputed later if the threshold changes - HR is flagged, not auto-rejected (Section 6
    /// offers both; auto-move risks silently rejecting on a scoring typo).</summary>
    public bool? Passed { get; set; }

    public string? ReviewerComments { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>Section 6's retake policy - set only on a failed score, from the posting's
    /// AssessmentRetakeCooldownMonths. Surfaced to HR, not automatically enforced across a future
    /// application, since "the same role" isn't something this system can reliably match.</summary>
    public DateOnly? RetakeAllowedAfter { get; set; }
}
