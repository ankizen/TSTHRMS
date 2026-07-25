using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TSTHRMS.Infrastructure.Identity;

namespace TSTHRMS.Infrastructure.Auth;

public class JwtTokenGenerator(IOptions<JwtSettings> options)
{
    private readonly JwtSettings _settings = options.Value;

    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("tenant_id", user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (user.EmployeeId is not null)
        {
            claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString()));
        }

        // HRBP-only scope narrowing - see ApplicationUser.AssignedLegalEntityId/AssignedProductId.
        if (user.AssignedLegalEntityId is not null)
        {
            claims.Add(new Claim("assigned_legal_entity_id", user.AssignedLegalEntityId.Value.ToString()));
        }

        if (user.AssignedProductId is not null)
        {
            claims.Add(new Claim("assigned_product_id", user.AssignedProductId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <summary>Section 3's Candidate Portal token - deliberately carries no role claims, so
    /// [Authorize(Roles=...)] staff endpoints reject it automatically while candidate-only
    /// endpoints (plain [Authorize] + ICandidateContext) accept it. No refresh token: a
    /// candidate's session is low-privilege and short-lived by design (a longer-lived access
    /// token only, re-requested via a fresh OTP once it expires) rather than mirroring staff's
    /// HttpOnly-cookie rotation, which would be disproportionate machinery for "check my
    /// application status".</summary>
    public (string Token, DateTimeOffset ExpiresAt) GenerateCandidateAccessToken(Guid candidateId, Guid tenantId)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var claims = new List<Claim>
        {
            new("candidate_id", candidateId.ToString()),
            new("tenant_id", tenantId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
