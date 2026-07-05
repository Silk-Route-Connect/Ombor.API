namespace Ombor.Contracts.Requests.Auth;

/// <summary>Request to start a password reset — sends a one-time code to the phone number if it has an account.</summary>
public sealed record ForgotPasswordRequest(string PhoneNumber);
