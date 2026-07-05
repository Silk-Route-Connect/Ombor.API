namespace Ombor.Contracts.Requests.Auth;

/// <summary>Request to check a password-reset code before the new-password step.</summary>
public sealed record VerifyResetCodeRequest(string PhoneNumber, string Code);
