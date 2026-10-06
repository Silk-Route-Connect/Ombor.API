namespace Ombor.Contracts.Responses.Auth;

/// <summary>Result of a password reset.</summary>
/// <param name="Success">Whether the password was reset.</param>
/// <param name="Message">Result/explanation text (English).</param>
/// <param name="Code">
/// Machine-readable failure reason when <paramref name="Success"/> is false: <c>auth.code_invalid</c>,
/// <c>auth.code_expired</c> or <c>auth.too_many_attempts</c> (the code was invalidated — request a new one).
/// </param>
public sealed record ResetPasswordResponse(bool Success, string? Message = null, string? Code = null);
