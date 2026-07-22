using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TSTHRMS.Application.Auth;
using TSTHRMS.Application.Auth.Dtos;
using TSTHRMS.Infrastructure.Identity;

namespace TSTHRMS.Infrastructure.Auth;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    JwtTokenGenerator tokenGenerator,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthResult.Failure("Invalid email or password.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthResult.Failure("Account is locked due to too many failed attempts. Try again later.");
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return AuthResult.Failure("Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(refreshToken);
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == tokenHash, cancellationToken);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTimeOffset.UtcNow)
        {
            return AuthResult.Failure("Invalid or expired refresh token.");
        }

        return await IssueTokensAsync(user);
    }

    public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        await userManager.UpdateAsync(user);
    }

    private async Task<AuthResult> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = tokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = JwtTokenGenerator.GenerateRefreshToken();

        // Rotation: overwriting the stored hash invalidates whatever refresh token was used to get here.
        user.RefreshTokenHash = Hash(refreshToken);
        user.RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);
        await userManager.UpdateAsync(user);

        var response = new LoginResponse(
            accessToken,
            expiresAt,
            new AuthenticatedUserDto(user.Id, user.Email ?? string.Empty, user.TenantId, roles.ToList()));

        return AuthResult.Success(response, refreshToken);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
