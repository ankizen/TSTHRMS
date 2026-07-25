using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 3's Candidate Portal login. Passwordless: a candidate requests a one-time code by
/// email and exchanges it for a session, since they authenticate rarely and a forgotten password
/// would be worse UX than a short-lived emailed code.
/// </summary>
public interface ICandidatePortalAuthService
{
    /// <summary>Always succeeds from the caller's perspective regardless of whether the email
    /// matches a candidate, to avoid leaking which emails have applied - it silently no-ops
    /// internally when there's no match.</summary>
    Task RequestOtpAsync(string email, CancellationToken cancellationToken = default);

    Task<CandidateLoginResultDto> VerifyOtpAsync(string email, string code, CancellationToken cancellationToken = default);
}
