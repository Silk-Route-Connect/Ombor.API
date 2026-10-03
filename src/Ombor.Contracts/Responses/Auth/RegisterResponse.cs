namespace Ombor.Contracts.Responses.Auth;

/// <summary>Acknowledges a registration start: the one-time code was sent to the phone number.</summary>
/// <param name="Message">Human-readable status (English; clients localize by their own text).</param>
/// <param name="ExpiresInMinutes">Code lifetime in minutes (5).</param>
/// <param name="CodeLength">Digits in the sent code (6 by default; configurable server-side).</param>
/// <param name="ResendAfterSeconds">Seconds before another code may be requested for this phone (resend cooldown).</param>
public sealed record RegisterResponse(string Message, int ExpiresInMinutes, int CodeLength, int ResendAfterSeconds);
