using System.ComponentModel.DataAnnotations;

namespace Ombor.Application.Configurations;

/// <summary>
/// One-time codes (registration + password reset). Every value has a production-safe default, so the section is
/// optional. The code lifetime is fixed at 5 minutes because the approved SMS templates state it.
/// </summary>
public sealed class OtpSettings
{
    public const string SectionName = nameof(OtpSettings);

    /// <summary>Code lifetime. Not configurable: the approved SMS templates say «5 daqiqa».</summary>
    public const int CodeLifetimeMinutes = 5;

    /// <summary>Digits per code. 6 by default; set 4 only if the SMS provider rejects 6-digit codes.</summary>
    [Range(4, 8, ErrorMessage = "OTP code length must be between 4 and 8 digits.")]
    public int CodeLength { get; init; } = 6;

    /// <summary>Minimum wait between two codes sent to the same phone for the same purpose.</summary>
    [Range(0, 3600, ErrorMessage = "OTP resend cooldown must be between 0 and 3600 seconds.")]
    public int ResendCooldownSeconds { get; init; } = 60;

    /// <summary>Codes one phone may receive per purpose in a rolling 24-hour window (SMS-cost cap).</summary>
    [Range(1, 1000, ErrorMessage = "OTP daily send limit must be between 1 and 1000.")]
    public int DailySendLimit { get; init; } = 10;

    /// <summary>Wrong guesses that invalidate an issued code.</summary>
    [Range(1, 100, ErrorMessage = "OTP max verify attempts must be between 1 and 100.")]
    public int MaxVerifyAttempts { get; init; } = 5;
}
