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

    public ICollection<LegalEntity> LegalEntities { get; set; } = new List<LegalEntity>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public enum TenantStatus
{
    Active,
    Suspended,
    Cancelled
}
