namespace Ombor.Contracts.Requests.Auth;

public sealed record SmsVerificationRequest(string Token, string Code);
