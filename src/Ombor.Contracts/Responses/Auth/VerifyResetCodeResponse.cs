namespace Ombor.Contracts.Responses.Auth;

/// <summary>Result of checking a password-reset code.</summary>
public sealed record VerifyResetCodeResponse(bool Success, string? Message = null);
