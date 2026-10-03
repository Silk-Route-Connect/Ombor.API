using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Exceptions;

namespace Ombor.API.ExceptionHandlers;

/// <summary>
/// Safety net for a constraint the service layer missed: maps the SQL Server errors a client can cause to a 4xx
/// instead of a 500 — a unique-index violation to 409 <c>conflict.duplicate</c>, a foreign-key violation to 409
/// <c>entity.referenced</c>, a too-long value to 400 <c>validation.failed</c>. Any other database failure stays a 500.
/// Reaching this handler still means a guard is missing, so it logs a warning (which reaches Sentry) rather than
/// staying silent like the plain 4xx handlers.
/// </summary>
internal sealed class DbUpdateExceptionHandler(ILogger<DbUpdateExceptionHandler> logger) : IExceptionHandler
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const int ForeignKeyViolation = 547;
    private const int StringTruncated = 2628;
    private const int StringTruncatedLegacy = 8152;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException { InnerException: SqlException sqlException })
        {
            return false;
        }

        var problemDetails = sqlException.Number switch
        {
            UniqueIndexViolation or UniqueConstraintViolation => Conflict(
                httpContext, "A record with the same value already exists.", ErrorCodes.ConflictDuplicate),
            ForeignKeyViolation => Conflict(
                httpContext, "The record is referenced by other records, or references a record that does not exist.", ErrorCodes.EntityReferenced),
            StringTruncated or StringTruncatedLegacy => new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "A value is longer than allowed.",
                Type = "https://httpstatuses.com/400",
                Instance = httpContext.Request.Path,
            }.WithCode(ErrorCodes.ValidationFailed),
            _ => null,
        };

        if (problemDetails is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        logger.LogWarning(
            exception,
            "Database constraint {SqlError} surfaced as {Status} on {Method} {Path} — a service-level guard is missing.",
            sqlException.Number,
            problemDetails.Status,
            httpContext.Request.Method,
            httpContext.Request.Path);

        return true;
    }

    private static ProblemDetails Conflict(HttpContext httpContext, string detail, string code) => new ProblemDetails
    {
        Title = "Conflict",
        Status = StatusCodes.Status409Conflict,
        Detail = detail,
        Type = "https://httpstatuses.com/409",
        Instance = httpContext.Request.Path,
    }.WithCode(code);
}
