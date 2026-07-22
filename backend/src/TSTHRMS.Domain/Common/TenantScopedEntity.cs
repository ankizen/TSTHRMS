namespace TSTHRMS.Domain.Common;

public abstract class TenantScopedEntity : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
}
