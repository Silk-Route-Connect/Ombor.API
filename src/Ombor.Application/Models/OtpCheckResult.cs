using Ombor.Domain.Exceptions;

namespace Ombor.Application.Models;

/// <summary>Outcome of checking a one-time code.</summary>
public enum OtpCheckResult
{
    Valid,
    Invalid,
    Expired,

    /// <summary>The wrong-guess budget is spent; the code was deleted and a new one must be requested.</summary>
    TooManyAttempts,
}

internal static class OtpCheckResultExtensions
{
    /// <summary>The <see cref="ErrorCodes">error code</see> a failed check is served with (null for a valid code).</summary>
    public static string? ToErrorCode(this OtpCheckResult result) => result switch
    {
        OtpCheckResult.Invalid => ErrorCodes.CodeInvalid,
        OtpCheckResult.Expired => ErrorCodes.CodeExpired,
        OtpCheckResult.TooManyAttempts => ErrorCodes.TooManyAttempts,
        _ => null,
    };
}
