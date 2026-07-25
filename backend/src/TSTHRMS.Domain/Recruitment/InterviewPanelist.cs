using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 14: "Interviewer" is a per-interview assignment, not a global role - any logged-in
/// user (Employee, Manager, whoever is picked) can be assigned here and gains scorecard access
/// scoped to exactly this interview, nothing else in the pipeline.
/// </summary>
public class InterviewPanelist : TenantScopedEntity
{
    public Guid InterviewId { get; set; }
    public Interview? Interview { get; set; }

    public Guid InterviewerUserId { get; set; }
}
