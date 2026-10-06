using Microsoft.Extensions.Logging;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;

namespace Ombor.Infrastructure.Services;

/// <summary>
/// Development stand-in for <see cref="SmsService"/>: writes the message (and so the OTP code) to the API log
/// instead of calling the SMS provider, so local registration and password-reset testing never sends real, billed SMS.
/// Opt back into real delivery with <c>SmsSettings:SendInDevelopment = true</c>.
/// </summary>
internal sealed class LoggingSmsService(
    IRequestValidator validator,
    ILogger<LoggingSmsService> logger) : ISmsService
{
    public async Task SendMessageAsync(SmsMessage message)
    {
        await validator.ValidateAndThrowAsync(message);

        logger.LogWarning(
            "SMS not sent (development log-only mode). To: {ToNumber}. Message: {Message}",
            message.ToNumber,
            message.Message);
    }
}
