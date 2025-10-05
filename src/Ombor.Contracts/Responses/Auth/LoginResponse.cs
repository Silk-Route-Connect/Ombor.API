namespace Ombor.Contracts.Responses.Auth;

public sealed record LoginResponse(string AccessToken, string RefreshToken);
