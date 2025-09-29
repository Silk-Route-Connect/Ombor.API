namespace Ombor.Contracts.Responses.Auth;

public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken);
