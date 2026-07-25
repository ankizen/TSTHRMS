using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Auth;
using TSTHRMS.Application.Auth.Dtos;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshCookieName = "tsthrms_refresh";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized(new { error = result.Error });
        }

        SetRefreshCookie(result.RefreshToken!);
        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { error = "No active session." });
        }

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);
        if (!result.Succeeded)
        {
            Response.Cookies.Delete(RefreshCookieName);
            return Unauthorized(new { error = result.Error });
        }

        SetRefreshCookie(result.RefreshToken!);
        return Ok(result.Response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userId, out var id))
        {
            await authService.LogoutAsync(id, cancellationToken);
        }

        Response.Cookies.Delete(RefreshCookieName);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            tenantId = User.FindFirst("tenant_id")?.Value,
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
        });
    }

    private void SetRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            // A split deployment (frontend on Vercel, API elsewhere, e.g. Coolify) puts the two
            // on different origins, and a cross-origin XHR never sends a Strict or Lax cookie at
            // all - only None does, which itself requires Secure (already true outside dev).
            SameSite = environment.IsDevelopment() ? SameSiteMode.Strict : SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/auth"
        });
    }
}
