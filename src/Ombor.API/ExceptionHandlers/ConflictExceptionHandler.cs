using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Exceptions;

namespace Ombor.API.ExceptionHandlers;

/// <summary>
/// Maps a <see cref="ConflictException"/> (reference-gated delete) to a 409 ProblemDetails.
/// </summary>
internal sealed class ConflictExceptionHandler(ILogger<ConflictExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ConflictException conflictException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = conflictException.Message,
            Type = "https://httpstatuses.com/409",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // 409s are client errors, not Sentry events — log locally only.
        logger.LogWarning(conflictException, "Conflict: {Message}", conflictException.Message);

        return true;
    }
}
