using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace Ombor.API.ExceptionHandlers;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment hostEnvironment,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            // Raw exception text (SQL timeouts, stack details) reaches the client only on a developer's machine;
            // everywhere else the body is generic and the traceId correlates it with the logged exception.
            Detail = hostEnvironment.IsDevelopment() ? exception.Message : "An error occurred while processing the request.",
            Type = "https://httpstatuses.com/500",
            Instance = httpContext.Request.Path
        };

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });

        SentrySdk.CaptureException(exception);

        logger.LogError(
            exception,
            "Unhandled exception processing {Method} {Path} (TraceId: {TraceId})",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        return true;
    }
}
