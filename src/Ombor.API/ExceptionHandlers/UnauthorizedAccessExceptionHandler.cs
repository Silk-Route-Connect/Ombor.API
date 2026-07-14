using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ombor.API.ExceptionHandlers;

/// <summary>
/// Maps an <see cref="UnauthorizedAccessException"/> (bad credentials, invalid/expired refresh token,
/// deactivated account) to a 401 ProblemDetails. A client auth failure is not a server fault, so it is
/// logged locally but not sent to Sentry.
/// </summary>
internal sealed class UnauthorizedAccessExceptionHandler(ILogger<UnauthorizedAccessExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not UnauthorizedAccessException unauthorizedException)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = unauthorizedException.Message,
            Type = "https://httpstatuses.com/401",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        logger.LogWarning(unauthorizedException, "Unauthorized: {Message}", unauthorizedException.Message);

        return true;
    }
}
