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

    /// <summary>Mirrors the refresh cookie's persistence choice so a later /refresh call - which
    /// only ever sees the refresh token itself, not how it was originally issued - knows whether
    /// to keep renewing a persistent cookie or a session-only one. Not sensitive, so it doesn't
    /// need its own secrecy, just the same lifetime as the cookie it describes.</summary>
    private const string RememberMeCookieName = "tsthrms_remember";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized(new { error = result.Error });
        }

        SetSessionCookies(result.RefreshToken!, request.RememberMe);
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

        var rememberMe = Request.Cookies[RememberMeCookieName] == "1";

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);
        if (!result.Succeeded)
        {
            Response.Cookies.Delete(RefreshCookieName);
            Response.Cookies.Delete(RememberMeCookieName);
            return Unauthorized(new { error = result.Error });
        }

        SetSessionCookies(result.RefreshToken!, rememberMe);
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
        Response.Cookies.Delete(RememberMeCookieName);
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

    private void SetSessionCookies(string refreshToken, bool rememberMe)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            // A split deployment (frontend on Vercel, API elsewhere, e.g. Coolify) puts the two
            // on different origins, and a cross-origin XHR never sends a Strict or Lax cookie at
            // all - only None does, which itself requires Secure (already true outside dev).
            SameSite = environment.IsDevelopment() ? SameSiteMode.Strict : SameSiteMode.None,
            Path = "/api/auth"
        };

        // "Keep me signed in" unchecked: no Expires at all means a browser-session cookie - it
        // survives page reloads/tab closes but disappears the moment the browser itself fully
        // closes, so the next visit requires signing in again. Checked: a normal persistent
        // cookie, same as this app's only previous (always-remembered) behavior.
        if (rememberMe)
        {
            options.Expires = DateTimeOffset.UtcNow.AddDays(7);
        }

        Response.Cookies.Append(RefreshCookieName, refreshToken, options);
        Response.Cookies.Append(RememberMeCookieName, rememberMe ? "1" : "0", options);
    }
}
