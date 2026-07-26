using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 11: the Day-1 checklist, created against the new Employee (not the Application) the
/// moment IOnboardingService.ConvertToEmployeeAsync runs - distinct from Section 10's pre-
/// boarding checklist, which is candidate-facing and runs before Day 1. Bare EmployeeId (no
/// navigation) for the same reason Employee.SourceApplicationId has no navigation back - Core HR
/// and Recruitment stay decoupled at the Domain level even though this table lives in the
/// Recruitment module that created it.
/// </summary>
public class OnboardingChecklistItem : TenantScopedEntity
{
    public Guid EmployeeId { get; set; }

    public OnboardingTaskType TaskType { get; set; }
    public Guid? OwnerUserId { get; set; }
    public DateOnly DueDate { get; set; }
    public OnboardingTaskStatus Status { get; set; } = OnboardingTaskStatus.Pending;
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
}

public enum OnboardingTaskType
{
    ItSetup,
    AccessProvisioning,
    InductionSession,
    PolicyAcknowledgement,
    BuddyAssignment
}

public enum OnboardingTaskStatus
{
    Pending,
    Completed
}
