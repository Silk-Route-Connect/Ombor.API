using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;

namespace Ombor.API.Controllers;

[Route("api/auth")]
[ApiController]
[AllowAnonymous]
public class AuthController(
    IAuthService service,
    IOptions<JwtSettings> jwt,
    IOptions<CookieSettings> cookieSettings,
    ILogger<AuthController> logger) : ControllerBase
{
    private const string RefreshTokenCookieName = "ombor.refreshToken";

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> RegisterAsync([FromBody] RegisterRequest request)
    {
        var result = await service.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerifyOtpResponse>> SmsVerificationAsync([FromBody] SmsVerificationRequest request)
    {
        var result = await service.VerifyRegistrationOtpAsync(request);

        if (!result.Success)
        {
            return BadRequest(new { message = "Invalid verification code." });
        }

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginAsync([FromBody] LoginRequest request)
    {
        logger.LogInformation("Login request received");

        var response = await service.LoginAsync(request);

        SetRefreshTokenCookie(response.RefreshToken);

        logger.LogInformation("Login successful, cookie set");

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshTokenAsync([FromBody] RefreshTokenRequest? request)
    {
        logger.LogInformation("Refresh token request received");
        logger.LogInformation("Request origin: {Origin}", Request.Headers["Origin"].ToString());
        logger.LogInformation("Cookies present: {Cookies}", string.Join(", ", Request.Cookies.Keys));

        var cookieToken = Request.Cookies[RefreshTokenCookieName];
        var token = cookieToken ?? request?.RefreshToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning(
                "No refresh token found. Cookie keys present: [{Cookies}]",
                string.Join(", ", Request.Cookies.Keys));

            return Unauthorized(new { message = "Refresh token is required" });
        }

        logger.LogInformation("Refresh token found from: {Source}",
            cookieToken != null ? "cookie" : "body");

        var result = await service.RefreshTokenAsync(new RefreshTokenRequest(token));

        SetRefreshTokenCookie(result.RefreshToken);

        logger.LogInformation("Refresh successful");

        return Ok(result);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> LogoutAsync()
    {
        var token = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrWhiteSpace(token))
        {
            await service.RevokeRefreshTokenAsync(new RevokeRefreshTokenRequest(token));
        }

        ClearRefreshTokenCookie();

        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var settings = cookieSettings.Value;

        var sameSite = settings.SameSite.ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "strict" => SameSiteMode.Strict,
            "lax" => SameSiteMode.Lax,
            _ => SameSiteMode.Lax
        };

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = sameSite,
            Path = "/",
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(jwt.Value.RefreshTokenExpiresInDays)
        };

        // Only set Domain if explicitly configured (production)
        if (!string.IsNullOrWhiteSpace(settings.Domain))
        {
            cookieOptions.Domain = settings.Domain;
        }

        logger.LogInformation(
            "Setting cookie - Domain: {Domain}, Secure: {Secure}, SameSite: {SameSite}, Path: {Path}",
            cookieOptions.Domain ?? "(request host)",
            cookieOptions.Secure,
            cookieOptions.SameSite,
            cookieOptions.Path);

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        var settings = cookieSettings.Value;

        var sameSite = settings.SameSite.ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "strict" => SameSiteMode.Strict,
            "lax" => SameSiteMode.Lax,
            _ => SameSiteMode.Lax
        };

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = sameSite,
            Path = "/",
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };

        if (!string.IsNullOrWhiteSpace(settings.Domain))
        {
            cookieOptions.Domain = settings.Domain;
        }

        Response.Cookies.Delete(RefreshTokenCookieName, cookieOptions);
    }
}