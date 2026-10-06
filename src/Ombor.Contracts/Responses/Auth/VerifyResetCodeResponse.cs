namespace Ombor.Contracts.Responses.Auth;

/// <summary>Result of checking a password-reset code.</summary>
/// <param name="Success">Whether the code is valid.</param>
/// <param name="Message">Explanation when invalid (English).</param>
/// <param name="Code">
/// Machine-readable failure reason when <paramref name="Success"/> is false: <c>auth.code_invalid</c>,
/// <c>auth.code_expired</c> or <c>auth.too_many_attempts</c> (the code was invalidated — request a new one).
/// </param>
public sealed record VerifyResetCodeResponse(bool Success, string? Message = null, string? Code = null);
