using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Exceptions;

namespace Ombor.API.ExceptionHandlers;

internal sealed class InvalidFileExceptionHandler(ILogger<InvalidFileException> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not InvalidFileException fileException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Provided file is not valid.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        logger.LogWarning(
            exception,
            "Error while processing file. {Message}",
            fileException.Message);

        return true;
    }
}
