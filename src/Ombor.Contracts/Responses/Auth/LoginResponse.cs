namespace Ombor.Contracts.Responses.Auth;

/// <summary>The tokens issued on a successful login, plus the user's saved interface language.</summary>
/// <param name="AccessToken">The JWT access token.</param>
/// <param name="RefreshToken">The refresh token (also set as an httpOnly cookie).</param>
/// <param name="Language">The user's saved interface language (ru / uz-Latn / uz-Cyrl), so a fresh device restores it.</param>
public sealed record LoginResponse(string AccessToken, string RefreshToken, string Language);
