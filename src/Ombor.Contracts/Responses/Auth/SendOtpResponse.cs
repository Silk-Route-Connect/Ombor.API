namespace Ombor.Contracts.Responses.Auth;

public sealed record SendOtpResponse(string Message, int ExpiresInMinutes);
