using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 7: structured, comparable feedback (rated criteria, not free-text "good/bad"). Rows
/// are create-only - Build Notes' append-only/no-editing-after-submission rule means there is no
/// update path, only insert; the row's mere existence is what "submitted" means.
/// </summary>
public class InterviewScorecard : TenantScopedEntity
{
    public Guid InterviewId { get; set; }
    public Interview? Interview { get; set; }

    public Guid InterviewerUserId { get; set; }

    public int TechnicalSkillsRating { get; set; }
    public int CommunicationRating { get; set; }
    public int ProblemSolvingRating { get; set; }
    public int CultureFitRating { get; set; }
    public InterviewRecommendation Recommendation { get; set; }
    public string? Comments { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
}

public enum InterviewRecommendation
{
    StrongYes,
    Yes,
    No,
    StrongNo
}
