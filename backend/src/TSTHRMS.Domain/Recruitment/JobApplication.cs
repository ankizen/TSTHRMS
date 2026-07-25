using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 5: one candidate's journey through one job posting's pipeline. A candidate who
/// applies to two postings gets two Application rows against the same Candidate row.
/// </summary>
public class JobApplication : TenantScopedEntity
{
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public Guid JobPostingId { get; set; }
    public JobPosting? JobPosting { get; set; }

    public ApplicationStage Stage { get; set; } = ApplicationStage.Applied;
    public DateTimeOffset StageChangedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset AppliedAt { get; set; }

    public ICollection<ApplicationStageHistory> StageHistory { get; set; } = new List<ApplicationStageHistory>();
}

/// <summary>Section 5's pipeline stages, in order. Assessment/interview-round entities that back
/// the middle stages arrive in later slices - the stage itself is tracked from Slice 1 on.</summary>
public enum ApplicationStage
{
    Applied,
    Screening,
    Assessment,
    InterviewRound1,
    InterviewRound2,
    InterviewRound3,
    Selected,
    Offer,
    OfferAccepted,
    Hired,
    Rejected,
    OnHold
}
