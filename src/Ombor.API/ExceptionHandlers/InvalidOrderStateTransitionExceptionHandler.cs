using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Exceptions;
using Sentry;

namespace Ombor.API.ExceptionHandlers;

/// <summary>
/// Maps an <see cref="InvalidOrderStateTransitionException"/> (an illegal order status change) to a 409 ProblemDetails.
/// </summary>
internal sealed class InvalidOrderStateTransitionExceptionHandler(
    ILogger<InvalidOrderStateTransitionExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not InvalidOrderStateTransitionException transitionException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Invalid order state transition",
            Status = StatusCodes.Status409Conflict,
            Detail = transitionException.Message,
            Type = "https://httpstatuses.com/409",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        SentrySdk.CaptureException(transitionException);

        logger.LogWarning(transitionException, "Invalid order state transition: {Message}", transitionException.Message);

        return true;
    }
}
