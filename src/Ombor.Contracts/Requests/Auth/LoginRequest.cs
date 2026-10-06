namespace Ombor.Contracts.Requests.Auth;

/// <summary>Credentials for signing in.</summary>
/// <param name="PhoneNumber">Uzbek phone number in any common format (normalized server-side to +998XXXXXXXXX).</param>
/// <param name="Password">The account password.</param>
public sealed record LoginRequest(string PhoneNumber, string Password);
