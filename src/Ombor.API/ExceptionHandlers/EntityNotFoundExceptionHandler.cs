using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Exceptions;
using Sentry;

namespace Ombor.API.ExceptionHandlers;

internal sealed class EntityNotFoundExceptionHandler(ILogger<EntityNotFoundExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not EntityNotFoundException entityNotFoundException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = entityNotFoundException.Message,
            Type = entityNotFoundException.ExceptionType,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        SentrySdk.CaptureException(entityNotFoundException);

        logger.LogWarning(
            exception,
            "{Type} with ID: {Id} was not found.",
            entityNotFoundException.EntityType,
            entityNotFoundException.Id);

        return true;
    }
}
