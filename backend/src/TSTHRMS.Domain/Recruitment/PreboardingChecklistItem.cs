using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 10: the checklist auto-created the moment an offer is accepted (see
/// OfferService.RespondPublicOfferAsync -> IPreboardingService.CreateChecklistAsync). Document
/// tasks map to the same categories Core HR itself uses (Education, Identity, Previous
/// Employment) so Slice 8's Day-1 conversion can carry them straight across without asking the
/// new hire to submit anything twice.
/// </summary>
public class PreboardingChecklistItem : TenantScopedEntity
{
    public Guid ApplicationId { get; set; }
    public JobApplication? Application { get; set; }

    public PreboardingTaskType TaskType { get; set; }
    public PreboardingTaskStatus Status { get; set; } = PreboardingTaskStatus.Pending;
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Who completed it - null for a candidate self-service submission, set for an
    /// HR/IT-side task like the IT asset request.</summary>
    public Guid? CompletedByUserId { get; set; }

    // Populated for the three document-upload tasks.
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }

    // Populated only for the BankDetails task.
    public string? BankAccountNumber { get; set; }
    public string? BankIfscCode { get; set; }
}

public enum PreboardingTaskType
{
    EducationCertificate,
    IdentityProof,
    PreviousEmploymentRelievingLetter,
    BankDetails,
    ItAssetRequest,
    WelcomeCommunication
}

public enum PreboardingTaskStatus
{
    Pending,
    Completed
}
