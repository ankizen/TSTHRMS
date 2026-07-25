using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 7: one scheduled interview round for one application. Reschedules update
/// ScheduledAt in place and bump RescheduleCount rather than creating a new row - the PDF's
/// "reschedule/no-show tracking" only needs a count and a status, not a full history table.
/// </summary>
public class Interview : TenantScopedEntity
{
    public Guid ApplicationId { get; set; }
    public JobApplication? Application { get; set; }

    /// <summary>Which pipeline round this interview is for - InterviewRound1/2/3.</summary>
    public ApplicationStage Round { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 45;
    public string? VideoLink { get; set; }
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
    public int RescheduleCount { get; set; }
    public Guid ScheduledByUserId { get; set; }

    public ICollection<InterviewPanelist> Panelists { get; set; } = new List<InterviewPanelist>();
    public ICollection<InterviewScorecard> Scorecards { get; set; } = new List<InterviewScorecard>();
}

public enum InterviewStatus
{
    Scheduled,
    Completed,
    NoShow,
    Cancelled
}
