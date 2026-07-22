using TSTHRMS.Application.Auth.Dtos;

namespace TSTHRMS.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>The refresh token alone identifies the user (looked up by hash), so a silent
    /// refresh works even after a full browser restart wiped any in-memory access token.</summary>
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
}
