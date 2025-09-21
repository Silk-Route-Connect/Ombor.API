namespace Ombor.Contracts.Requests.Auth;

public sealed record RefreshTokenRequest(string AccessToken, string RefreshToken);