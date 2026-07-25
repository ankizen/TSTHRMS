using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 3's Candidate Portal login - passwordless by design, since a candidate logs in rarely
/// and a password they'd forget is worse UX than a short-lived emailed code. The code is hashed
/// (never stored plain), same spirit as a password hash, even though it's single-use and expires
/// quickly.
/// </summary>
public class CandidateOtp : TenantScopedEntity
{
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    public required string CodeHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}
