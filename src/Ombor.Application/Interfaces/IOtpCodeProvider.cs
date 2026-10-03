using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Domain.Enums;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Issues and checks one-time codes, keyed by canonical phone + purpose, and enforces the per-phone protections:
/// a resend cooldown and a rolling daily send cap (SMS cost), and a wrong-guess budget per issued code.
/// </summary>
public interface IOtpCodeProvider
{
    /// <summary>Digits in every issued code (<c>OtpSettings:CodeLength</c>).</summary>
    int CodeLength { get; }

    /// <summary>Seconds a client waits before it may request another code for the same phone + purpose.</summary>
    int ResendAfterSeconds { get; }

    /// <summary>
    /// Spends one send from the phone's budget for <paramref name="purpose"/>. Throws
    /// <see cref="Ombor.Domain.Exceptions.TooManyRequestsException"/> inside the resend cooldown or past the daily cap.
    /// </summary>
    Task EnsureCanSendAsync(string phoneNumber, OtpPurpose purpose);

    /// <summary>Stores a fresh code (replacing any previous one and its wrong-guess count) and returns it.</summary>
    Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose);

    Task<OtpCode?> GetOtpAsync(string phoneNumber, OtpPurpose purpose);

    /// <summary>
    /// Checks <paramref name="code"/>. Every check counts against the issued code's budget before the comparison, so
    /// parallel guesses cannot exceed it; the code is deleted once the budget is spent. A correct code resets the count.
    /// </summary>
    Task<OtpCheckResult> VerifyOtpAsync(string phoneNumber, OtpPurpose purpose, string code);

    Task<RegisterRequest?> GetRegisterRequestAsync(string phoneNumber);
    Task SetRegisterRequestAsync(RegisterRequest request, TimeSpan lifetime);
    Task RemoveOtpAsync(string phoneNumber, OtpPurpose purpose);
    Task RemoveRegisterRequestAsync(string phoneNumber);
}
