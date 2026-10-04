namespace Ombor.Contracts.Responses.Auth;

/// <summary>
/// A successful login: the access token plus the user's saved interface language. The refresh token travels only in
/// the httpOnly <c>ombor.refreshToken</c> cookie, never in the body.
/// </summary>
/// <param name="AccessToken">The JWT access token.</param>
/// <param name="Language">The user's saved interface language (ru / uz-Latn / uz-Cyrl), so a fresh device restores it.</param>
public sealed record LoginResponse(string AccessToken, string Language);
