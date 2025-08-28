namespace Ombor.Contracts.Requests.Auth;

public sealed record LoginRequest(string phoneNumber, string password);

