using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Auth;

/// <summary>
/// Lives in Infrastructure (not alongside the other recruitment services in Application) purely
/// because it needs the concrete JwtTokenGenerator to mint a session token - same reason
/// AuthService lives here rather than in Application.Auth.
/// </summary>
public class CandidatePortalAuthService(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IEmailSender emailSender,
    JwtTokenGenerator jwtTokenGenerator,
    ILogger<CandidatePortalAuthService> logger) : ICandidatePortalAuthService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    public async Task RequestOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.Candidates.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        if (candidate is null)
        {
            return;
        }

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        dbContext.CandidateOtps.Add(new CandidateOtp
        {
            CandidateId = candidate.Id,
            CodeHash = Hash(code),
            ExpiresAt = DateTimeOffset.UtcNow.Add(CodeLifetime),
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var html = $"""
                <p>Hi {WebUtility.HtmlEncode(candidate.FirstName)},</p>
                <p>Your sign-in code is:</p>
                <p style="font-size: 24px; font-weight: 600; letter-spacing: 4px;">{code}</p>
                <p>This code expires in 10 minutes.</p>
                """;
            await emailSender.SendAsync(candidate.Email, "Your sign-in code", html, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send candidate OTP email to candidate {CandidateId}", candidate.Id);
        }
    }

    public async Task<CandidateLoginResultDto> VerifyOtpAsync(
        string email, string code, CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.Candidates.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        if (candidate is null)
        {
            return new CandidateLoginResultDto(false, null, null, null);
        }

        var codeHash = Hash(code);
        var otp = await dbContext.CandidateOtps
            .Where(o => o.CandidateId == candidate.Id && o.CodeHash == codeHash && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || DateTimeOffset.UtcNow > otp.ExpiresAt)
        {
            return new CandidateLoginResultDto(false, null, null, null);
        }

        otp.ConsumedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = jwtTokenGenerator.GenerateCandidateAccessToken(candidate.Id, tenantContext.TenantId);
        return new CandidateLoginResultDto(true, token, expiresAt, $"{candidate.FirstName} {candidate.LastName}");
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
