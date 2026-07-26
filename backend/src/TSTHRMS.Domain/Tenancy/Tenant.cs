using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Tenancy;

/// <summary>
/// A subscribing customer/company. Your own company is the first tenant seeded at setup;
/// each company that later buys the product becomes a new, fully isolated tenant.
/// </summary>
public class Tenant : AuditableEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    /// <summary>Section 13 (DPDPA 2023): how long a Rejected candidate's data is kept before the
    /// automatic retention sweep anonymizes it. A candidate tagged IsInTalentPool is exempt -
    /// they opted to be kept in mind.</summary>
    public int RejectedCandidateRetentionDays { get; set; } = 180;

    /// <summary>Section 4: null means "not configured yet" - referrals simply don't become
    /// bonus-eligible until an HRAdmin sets this.</summary>
    public decimal? ReferralBonusAmount { get; set; }

    /// <summary>Section 8: merge-variable offer letter template (e.g. {{CandidateName}},
    /// {{Designation}}, {{AnnualCtc}}, {{DateOfJoining}}, {{CompanyName}}). Null falls back to
    /// OfferService's hardcoded default text, unchanged from before this existed.</summary>
    public string? OfferLetterTemplate { get; set; }

    public ICollection<LegalEntity> LegalEntities { get; set; } = new List<LegalEntity>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public enum TenantStatus
{
    Active,
    Suspended,
    Cancelled
}
