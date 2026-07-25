using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 8: one immutable snapshot of an offer's terms. Revising an offer always adds a new
/// row with an incremented <see cref="VersionNumber"/> rather than editing an existing one - the
/// "negotiation log" the PDF asks for is just this table's full history for the Offer.
/// </summary>
public class OfferVersion : TenantScopedEntity
{
    public Guid OfferId { get; set; }
    public Offer? Offer { get; set; }

    public int VersionNumber { get; set; }
    public string? Designation { get; set; }
    public DateOnly DateOfJoining { get; set; }

    /// <summary>Section 8: "pre-filled with entity-specific CTC structure (pulls from the same
    /// CTC logic used in Payroll)" - Payroll doesn't exist yet (a later phase), so this is a
    /// simple breakdown for now; wire it to the real CTC engine once that phase lands.</summary>
    public decimal AnnualCtc { get; set; }
    public decimal? FixedComponent { get; set; }
    public decimal? VariableComponent { get; set; }
    public decimal? JoiningBonus { get; set; }

    public string? OfferLetterText { get; set; }
    public string? RevisionReason { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
