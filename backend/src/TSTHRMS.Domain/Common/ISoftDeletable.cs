namespace TSTHRMS.Domain.Common;

/// <summary>
/// Section 15: Core HR child records (Education, Family, Previous Employment, Identity
/// Documents, Nominees) are never hard-deleted - marking this interface wires the entity into
/// ApplicationDbContext's global query filter convention automatically, the same way
/// ITenantScoped does for multi-tenancy. Employee itself doesn't need this: it's already
/// soft-delete-only via EmployeeStatus.Exited, no boolean flag required.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
