namespace Ombor.Contracts.Responses.Auth;

/// <summary>
/// Acknowledges a password-reset request. The message is deliberately generic — it never reveals whether the
/// phone number has an account.
/// </summary>
public sealed record ForgotPasswordResponse(string Message, int ExpiresInMinutes);
