namespace TSTHRMS.Application.Auth.Dtos;

public record LoginRequest(string Email, string Password, bool RememberMe = false);

public record AuthenticatedUserDto(Guid Id, string Email, Guid TenantId, IReadOnlyList<string> Roles);

/// <summary>Body returned to the client. The refresh token never appears here - it only ever
/// travels as an HttpOnly cookie, so it's inaccessible to JS even if the page is XSS'd.</summary>
public record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    AuthenticatedUserDto User);

public record AuthResult(bool Succeeded, LoginResponse? Response, string? RefreshToken, string? Error)
{
    public static AuthResult Success(LoginResponse response, string refreshToken) => new(true, response, refreshToken, null);
    public static AuthResult Failure(string error) => new(false, null, null, error);
}
