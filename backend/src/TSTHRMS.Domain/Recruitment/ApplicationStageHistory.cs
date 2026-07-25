using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Build Notes: "Keep interview feedback and rejection reasons in an append-only log, not
/// editable after submission" - every stage transition (including the reason on a rejection)
/// is recorded here permanently rather than overwriting a single "current reason" field.
/// </summary>
public class ApplicationStageHistory : TenantScopedEntity
{
    public Guid ApplicationId { get; set; }
    public JobApplication? Application { get; set; }

    public ApplicationStage FromStage { get; set; }
    public ApplicationStage ToStage { get; set; }
    public string? Reason { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
