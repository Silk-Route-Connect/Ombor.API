using System.Diagnostics.CodeAnalysis;

namespace Ombor.Application.Models;

/// <summary>
/// A session the auth flow opened. The controller puts the access token in the response body and the refresh token
/// only into the httpOnly cookie — never into a body, where page script could read it (backend-12).
/// </summary>
/// <param name="AccessToken">The JWT access token.</param>
/// <param name="RefreshToken">The refresh token, for the cookie.</param>
/// <param name="Language">The user's saved interface language.</param>
public sealed record AuthSession(string AccessToken, string RefreshToken, string Language);

/// <summary>Outcome of confirming the registration code: the first session, or why the code was refused.</summary>
/// <param name="Session">The new account's session; null when the code was refused.</param>
/// <param name="ErrorCode">
/// <c>auth.code_invalid</c>, <c>auth.code_expired</c> or <c>auth.too_many_attempts</c> when refused.
/// </param>
public sealed record RegistrationVerification(AuthSession? Session, string? ErrorCode)
{
    [MemberNotNullWhen(true, nameof(Session))]
    public bool Success => Session is not null;
}
