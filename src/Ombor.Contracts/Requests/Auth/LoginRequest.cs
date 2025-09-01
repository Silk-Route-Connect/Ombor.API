namespace Ombor.Contracts.Requests.Auth;

public sealed record LoginRequest(string PhoneNumber, string Password);

