namespace Ombor.Contracts.Responses.Auth;

/// <summary>
/// A refreshed session: the new access token. The rotated refresh token travels only in the httpOnly
/// <c>ombor.refreshToken</c> cookie, never in the body.
/// </summary>
/// <param name="AccessToken">The new JWT access token.</param>
public sealed record RefreshTokenResponse(string AccessToken);
