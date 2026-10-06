namespace Ombor.Contracts.Requests.Auth;

/// <summary>
/// The refresh token to rotate — the service input of <c>POST /api/auth/refresh-token</c>, filled from the httpOnly
/// cookie (a request body is not read).
/// </summary>
/// <param name="RefreshToken">The refresh token from the cookie.</param>
public sealed record RefreshTokenRequest(string RefreshToken);
