using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Serialization;

namespace Ombor.API.ExceptionHandlers;

/// <summary>
/// Maps an <see cref="InvalidEnumValueException"/> (an unparseable enum value in a request body) to a 400
/// <see cref="ValidationProblemDetails"/>, matching the shape produced by <see cref="ValidationExceptionHandler"/>.
/// Without this the invalid value would reach the action as a null argument and surface as a 500.
/// </summary>
internal sealed class InvalidEnumExceptionHandler(ILogger<InvalidEnumExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not InvalidEnumValueException enumException)
        {
            return false;
        }

        var problem = new ValidationProblemDetails
        {
            Title = "One or more validation errors occurred.",
            Detail = enumException.Message,
            Status = StatusCodes.Status400BadRequest,
            Type = "https://httpstatuses.com/400",
            Instance = httpContext.Request.Path,
            Errors = new Dictionary<string, string[]>
            {
                [enumException.EnumTypeName] = [enumException.Message]
            }
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        logger.LogWarning(enumException, "Invalid enum value in request: {Message}", enumException.Message);

        return true;
    }
}
