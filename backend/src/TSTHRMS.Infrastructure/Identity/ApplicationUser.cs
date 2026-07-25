using Microsoft.AspNetCore.Identity;

namespace TSTHRMS.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }

    /// <summary>Linked once the Core HR Employee record exists for this user (Phase 1).</summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>Only meaningful for the HRBP role - narrows that HRBP's access to a single legal
    /// entity and/or product. Null means unrestricted on that dimension.</summary>
    public Guid? AssignedLegalEntityId { get; set; }
    public Guid? AssignedProductId { get; set; }

    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
}
