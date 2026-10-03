using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Ombor.Domain.Exceptions;

namespace Ombor.API.ExceptionHandlers;

/// <summary>
/// Maps a <see cref="TooManyRequestsException"/> (a service-level throttle: OTP resend cooldown, daily cap, login
/// lockout) to a 429 ProblemDetails. The IP rate limiter answers with the same body via <see cref="WriteAsync"/>.
/// </summary>
internal sealed class TooManyRequestsExceptionHandler(ILogger<TooManyRequestsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not TooManyRequestsException throttled)
        {
            return false;
        }

        await WriteAsync(httpContext, throttled.RetryAfterSeconds, cancellationToken);

        // Throttling is expected client behaviour, not a server fault — log locally only.
        logger.LogWarning("Throttled {Path}: {Message}", httpContext.Request.Path, throttled.Message);

        return true;
    }

    /// <summary>Writes the agreed 429 body (<c>code: auth.rate_limited</c>, <c>params.retryAfterSeconds</c>) plus <c>Retry-After</c>.</summary>
    public static Task WriteAsync(HttpContext httpContext, int retryAfterSeconds, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Title = "Too Many Requests",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = "Too many requests. Try again later.",
            Type = "https://httpstatuses.com/429",
            Instance = httpContext.Request.Path
        }.WithCode(
            ErrorCodes.RateLimited,
            new Dictionary<string, object?> { ["retryAfterSeconds"] = retryAfterSeconds });

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.Headers[HeaderNames.RetryAfter] = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }
}
