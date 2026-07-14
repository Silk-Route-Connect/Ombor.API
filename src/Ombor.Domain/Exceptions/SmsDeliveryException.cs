namespace Ombor.Domain.Exceptions;

/// <summary>
/// Raised when the SMS provider is unreachable or rejects a message (e.g. an unmoderated template).
/// Surfaces as HTTP 503 so a failed OTP send reads as "try again later", not an opaque 500.
/// </summary>
public sealed class SmsDeliveryException : Exception
{
    public SmsDeliveryException(string message) : base(message) { }

    public SmsDeliveryException(string message, Exception? innerException) : base(message, innerException) { }
}
