namespace Ombor.Contracts.Responses.Auth;

public sealed record RegisterResponse(string Message, int ExpiresInMinutes);
