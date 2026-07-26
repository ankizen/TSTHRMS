using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 9: BGV status tracker, one per application. VendorReference is the "leave an API hook
/// for later" placeholder (Section 9's AuthBridge/IDfy-style integration) - a real vendor
/// callback would land here as a case reference, but scoring/checking is entirely manual for now.
/// Deliberately kept alive on the Application even after Hired, since the PDF's own "why this
/// matters" note is that a post-joining discrepancy needs somewhere to still show up.
/// </summary>
public class BackgroundVerification : TenantScopedEntity
{
    public Guid ApplicationId { get; set; }
    public JobApplication? Application { get; set; }

    public BgvStatus Status { get; set; } = BgvStatus.NotStarted;
    public string? VendorReference { get; set; }

    /// <summary>Section 9: lets joining proceed while verification is still running.</summary>
    public bool IsConditionalJoining { get; set; }

    public DateTimeOffset? InitiatedAt { get; set; }
    public DateTimeOffset? ClearedAt { get; set; }
    public string? DiscrepancyNotes { get; set; }

    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public enum BgvStatus
{
    NotStarted,
    Initiated,
    InProgress,
    Clear,
    DiscrepancyFound
}
