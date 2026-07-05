namespace Ombor.Contracts.Responses.Auth;

/// <summary>Result of a password reset.</summary>
public sealed record ResetPasswordResponse(bool Success, string? Message = null);
