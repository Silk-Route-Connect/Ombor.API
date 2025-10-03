namespace Ombor.Contracts.Requests.Auth;

public sealed record SmsVerificationRequest(string PhoneNumber, string Code);
