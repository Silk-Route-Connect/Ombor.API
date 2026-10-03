using System.Diagnostics.CodeAnalysis;

namespace Ombor.Contracts.Responses.Auth;

/// <summary>Result of confirming the registration code; carries the first session's tokens on success.</summary>
public sealed record VerifyOtpResponse
{
    [MemberNotNullWhen(true, nameof(RefreshToken), nameof(AccessToken))]
    public bool Success { get; }
    public string? RefreshToken { get; init; }
    public string? AccessToken { get; init; }

    /// <summary>
    /// Machine-readable failure reason when <see cref="Success"/> is false: <c>auth.code_invalid</c>,
    /// <c>auth.code_expired</c> or <c>auth.too_many_attempts</c>. Served in the 400 body as <c>code</c>.
    /// </summary>
    public string? Code { get; init; }

    public VerifyOtpResponse(string refreshToken, string accessToken)
    {
        RefreshToken = refreshToken;
        AccessToken = accessToken;
        Success = true;
    }

    public VerifyOtpResponse()
    {
        Success = false;
        RefreshToken = null;
        AccessToken = null;
    }
}
