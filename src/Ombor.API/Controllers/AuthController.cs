using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Ombor.API.Extensions;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Localization;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Exceptions;

namespace Ombor.API.Controllers;

[Route("api/auth")]
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public class AuthController(
    IAuthService service,
    IPasswordResetService passwordResetService,
    IOptions<JwtSettings> jwt,
    IOptions<CookieSettings> cookieSettings,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>The httpOnly cookie carrying the refresh token (also read by change-password to keep this session).</summary>
    internal const string RefreshTokenCookieName = "ombor.refreshToken";

    /// <summary>The header carrying the user's interface language on the registration flow.</summary>
    private const string LanguageHeaderName = "X-Ombor-Language";

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Sms)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> RegisterAsync([FromBody] RegisterRequest request)
    {
        // Reject a missing/unsupported language before an OTP is sent; the value is persisted to
        // User.Language and drives the starter-data names at verification.
        ResolveLanguageOrThrow();

        var result = await service.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerifyOtpResponse>> SmsVerificationAsync([FromBody] SmsVerificationRequest request)
    {
        var result = await service.VerifyRegistrationOtpAsync(request, ResolveLanguageOrThrow());

        if (!result.Success)
        {
            // Kept as a plain body (not ProblemDetails) for existing clients; `code` tells invalid / expired /
            // too many attempts apart.
            return BadRequest(new { message = "Invalid verification code.", code = result.ErrorCode });
        }

        SetRefreshTokenCookie(result.Session.RefreshToken);

        return Ok(new VerifyOtpResponse(result.Session.AccessToken));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginAsync([FromBody] LoginRequest request)
    {
        logger.LogInformation("Login request received");

        var session = await service.LoginAsync(request);

        SetRefreshTokenCookie(session.RefreshToken);

        logger.LogInformation("Login successful, cookie set");

        return Ok(new LoginResponse(session.AccessToken, session.Language));
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting(RateLimitPolicies.Session)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshTokenAsync()
    {
        logger.LogInformation("Refresh token request received");
        logger.LogInformation("Request origin: {Origin}", Request.Headers["Origin"].ToString());
        logger.LogInformation("Cookies present: {Cookies}", string.Join(", ", Request.Cookies.Keys));

        // The cookie is the only carrier (backend-12): a token in a request body is not read.
        var token = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning(
                "No refresh token found. Cookie keys present: [{Cookies}]",
                string.Join(", ", Request.Cookies.Keys));

            return Unauthorized(new { message = "Refresh token is required", code = ErrorCodes.SessionExpired });
        }

        var session = await service.RefreshTokenAsync(new RefreshTokenRequest(token));

        SetRefreshTokenCookie(session.RefreshToken);

        logger.LogInformation("Refresh successful");

        return Ok(new RefreshTokenResponse(session.AccessToken));
    }

    [HttpPost("logout")]
    [EnableRateLimiting(RateLimitPolicies.Session)]
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

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Sms)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request)
    {
        var result = await passwordResetService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    [HttpPost("verify-reset-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerifyResetCodeResponse>> VerifyResetCodeAsync([FromBody] VerifyResetCodeRequest request)
    {
        var result = await passwordResetService.VerifyResetCodeAsync(request);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPasswordAsync([FromBody] ResetPasswordRequest request)
    {
        var result = await passwordResetService.ResetPasswordAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Reads and validates the interface-language header. Throws a <see cref="ValidationException"/>
    /// (→ 400) when it is missing or not one of the supported languages — never falls back to a default.
    /// </summary>
    private string ResolveLanguageOrThrow()
    {
        var language = Request.Headers[LanguageHeaderName].ToString();

        if (!SupportedLanguages.IsSupported(language))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    LanguageHeaderName,
                    $"The '{LanguageHeaderName}' header is required and must be one of: {string.Join(", ", SupportedLanguages.All)}."),
            ]);
        }

        return language;
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