namespace Ombor.Application.Models;

public sealed record SmsMessage(string ToNumber, string Message, string Subject);
