namespace TSTHRMS.Infrastructure.Persistence;

/// <summary>
/// Purely technical plumbing for ISequenceGenerator - not a business entity, so it lives
/// here rather than in Domain, and deliberately isn't ITenantScoped: SequenceGenerator always
/// scopes its own raw SQL by tenant explicitly, since global query filters don't compose
/// reliably with the FOR UPDATE locking read it needs.
/// </summary>
public class TenantSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public long NextValue { get; set; } = 1;
}
