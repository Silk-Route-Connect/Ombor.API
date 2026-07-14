using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Exceptions;
using Sentry;

namespace Ombor.API.ExceptionHandlers;

/// <summary>
/// Maps a <see cref="SmsDeliveryException"/> to a 503 ProblemDetails. The client gets a generic
/// "try again later" message; the provider's detailed reason stays in logs/Sentry.
/// </summary>
internal sealed class SmsDeliveryExceptionHandler(ILogger<SmsDeliveryExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not SmsDeliveryException smsException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Service Unavailable",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = "SMS service is temporarily unavailable. Please try again later.",
            Type = "https://httpstatuses.com/503",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // A provider outage is a real operational failure worth a Sentry event (5xx, not user-caused).
        SentrySdk.CaptureException(smsException);

        logger.LogError(smsException, "SMS delivery failed: {Message}", smsException.Message);

        return true;
    }
}
