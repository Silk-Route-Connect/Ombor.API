using System.ComponentModel.DataAnnotations;

namespace Ombor.Application.Configurations;

/// <summary>
/// Throttling for the anonymous auth surface: per-IP rate-limiter windows (fixed window, one bucket per client IP
/// and policy) and the per-phone login lockout. Defaults are production-safe, so the section is optional; the
/// Testing environment raises the IP limits so the integration suite is never throttled by accident.
/// </summary>
public sealed class AuthSecuritySettings
{
    public const string SectionName = nameof(AuthSecuritySettings);

    /// <summary>Requests per IP per window on login, verification, verify-reset-code, reset-password, change-password.</summary>
    [Range(1, int.MaxValue)]
    public int AuthPermitLimit { get; init; } = 20;

    [Range(1, 86_400)]
    public int AuthWindowSeconds { get; init; } = 60;

    /// <summary>Requests per IP per window on the endpoints that send a (billed) SMS: register, forgot-password.</summary>
    [Range(1, int.MaxValue)]
    public int SmsPermitLimit { get; init; } = 10;

    [Range(1, 86_400)]
    public int SmsWindowSeconds { get; init; } = 3600;

    /// <summary>Requests per IP per window on refresh-token and logout (several users may share one shop IP).</summary>
    [Range(1, int.MaxValue)]
    public int SessionPermitLimit { get; init; } = 120;

    [Range(1, 86_400)]
    public int SessionWindowSeconds { get; init; } = 60;

    /// <summary>Failed password checks per phone allowed inside one lockout window; further attempts get 429.</summary>
    [Range(1, 1000)]
    public int MaxFailedLogins { get; init; } = 10;

    [Range(1, 1440)]
    public int FailedLoginWindowMinutes { get; init; } = 15;
}
