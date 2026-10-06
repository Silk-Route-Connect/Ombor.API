using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ombor.API.ExceptionHandlers;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Domain.Exceptions;

namespace Ombor.API.Extensions;

/// <summary>Names of the per-IP rate-limit policies applied with <c>[EnableRateLimiting]</c>.</summary>
internal static class RateLimitPolicies
{
    /// <summary>Password and code checks: login, verification, verify-reset-code, reset-password, change-password.</summary>
    public const string Auth = "auth";

    /// <summary>Endpoints that send a billed SMS: register, forgot-password. Strictest.</summary>
    public const string Sms = "auth-sms";

    /// <summary>Session upkeep: refresh-token, logout. Loosest — one shop IP may serve several users.</summary>
    public const string Session = "auth-session";
}

/// <summary>
/// The auth security baseline wired at the HTTP edge: per-IP rate limiting on the anonymous auth surface and the
/// rule-41 check on every bearer token. Per-phone protections (OTP cooldown/cap/attempts, login lockout) live in
/// the Application services.
/// </summary>
internal static class AuthSecurityExtensions
{
    public static IServiceCollection AddAuthSecurity(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
                    ? value
                    : TimeSpan.FromSeconds(1);

                return new ValueTask(TooManyRequestsExceptionHandler.WriteAsync(
                    context.HttpContext,
                    Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)),
                    cancellationToken));
            };

            options.AddPolicy(RateLimitPolicies.Auth, http => PerClientIp(http, s => (s.AuthPermitLimit, s.AuthWindowSeconds)));
            options.AddPolicy(RateLimitPolicies.Sms, http => PerClientIp(http, s => (s.SmsPermitLimit, s.SmsWindowSeconds)));
            options.AddPolicy(RateLimitPolicies.Session, http => PerClientIp(http, s => (s.SessionPermitLimit, s.SessionWindowSeconds)));
        });

        // PostConfigure: runs after Infrastructure's AddJwtBearer configuration, whatever the registration order.
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events ??= new JwtBearerEvents();
            options.Events.OnTokenValidated = RejectInactiveUserAsync;
            options.Events.OnChallenge = WriteCodedChallengeAsync;
        });

        return services;
    }

    // The client IP comes from the forwarded-headers middleware (runs earlier in the pipeline).
    private static RateLimitPartition<string> PerClientIp(
        HttpContext httpContext,
        Func<AuthSecuritySettings, (int PermitLimit, int WindowSeconds)> select)
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptions<AuthSecuritySettings>>().Value;
        var (permitLimit, windowSeconds) = select(settings);
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    // Rule 41: an access token stays cryptographically valid until it expires, so deactivation is enforced here —
    // a deactivated user's token stops working on the next request (immediately on this instance; within the
    // active-user cache window on others).
    private static async Task RejectInactiveUserAsync(TokenValidatedContext context)
    {
        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
        {
            context.Fail(new AuthenticationFailedException(ErrorCodes.SessionExpired, "The access token has no user."));
            return;
        }

        var activeUsers = context.HttpContext.RequestServices.GetRequiredService<IActiveUserCache>();

        if (!await activeUsers.IsActiveAsync(userId, context.HttpContext.RequestAborted))
        {
            context.Fail(new AuthenticationFailedException(ErrorCodes.AccountDeactivated, "This account has been deactivated."));
        }
    }

    // Default challenges answer with an empty 401; a coded rejection (deactivated user) gets the agreed body instead.
    private static async Task WriteCodedChallengeAsync(JwtBearerChallengeContext context)
    {
        if (context.AuthenticateFailure is not AuthenticationFailedException failure)
        {
            return;
        }

        context.HandleResponse();

        var problemDetails = new ProblemDetails
        {
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = failure.Message,
            Type = "https://httpstatuses.com/401",
            Instance = context.Request.Path
        }.WithCode(failure.Code);

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(problemDetails, context.HttpContext.RequestAborted);
    }
}
