using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Exceptions;

namespace Ombor.API.ExceptionHandlers;

internal sealed class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails
        {
            Title = "One or more validation errors occurred.",
            Detail = validationException.Message,
            Status = StatusCodes.Status400BadRequest,
            Type = "https://httpstatuses.com/400",
            Instance = httpContext.Request.Path,
            Errors = errors
        };

        // A failure raised with a domain code (CodedValidation / .WithErrorCode) names the whole error; plain rule
        // failures carry FluentValidation's built-in codes, which are not part of the contract.
        var codedFailure = validationException.Errors.FirstOrDefault(e => ErrorCodes.IsDomainCode(e.ErrorCode));
        problem.WithCode(
            codedFailure?.ErrorCode ?? ErrorCodes.ValidationFailed,
            codedFailure?.CustomState as IReadOnlyDictionary<string, object?>);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        // 400s are client errors, not Sentry events — log locally only.
        logger.LogWarning(validationException, "Validation failed: {Errors}", errors);

        return true;
    }
}
