namespace Ombor.Contracts.Responses.Auth;

/// <summary>
/// Acknowledges a password-reset request. The message is deliberately generic — it never reveals whether the
/// phone number has an account.
/// </summary>
/// <param name="Message">Generic status text.</param>
/// <param name="ExpiresInMinutes">Reset-code lifetime in minutes (5).</param>
/// <param name="CodeLength">Digits in the reset code (6 by default; configurable server-side).</param>
/// <param name="ResendAfterSeconds">Seconds before another code may be requested for this phone (resend cooldown).</param>
public sealed record ForgotPasswordResponse(string Message, int ExpiresInMinutes, int CodeLength, int ResendAfterSeconds);
