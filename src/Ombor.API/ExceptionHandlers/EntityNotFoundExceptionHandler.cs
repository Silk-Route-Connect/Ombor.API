using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Common;
using Ombor.Domain.Exceptions;

namespace Ombor.API.ExceptionHandlers;

internal sealed class EntityNotFoundExceptionHandler(ILogger<EntityNotFoundExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not EntityNotFoundException<EntityBase> entityNotFoundException)
        {
            return false;
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        var problemDetails = new ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = entityNotFoundException.Message,
            Type = entityNotFoundException.ExceptionType,
            Instance = httpContext.Request.Path
        };

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = entityNotFoundException
        });

        logger.LogWarning(
            exception,
            "{Type} entity with Id: {Id} was not found.",
            entityNotFoundException.EntityType,
            entityNotFoundException.Id);

        return true;
    }
}
