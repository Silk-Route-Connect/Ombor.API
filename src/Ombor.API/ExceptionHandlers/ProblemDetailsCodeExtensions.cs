using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Exceptions;

namespace Ombor.API.ExceptionHandlers;

/// <summary>
/// Adds the agreed machine-readable error members to a ProblemDetails body: <c>code</c> (one of
/// <see cref="ErrorCodes"/>) and, when present, <c>params</c>. Every exception handler that answers a domain error
/// goes through here so the shape stays identical.
/// </summary>
internal static class ProblemDetailsCodeExtensions
{
    public const string CodeKey = "code";
    public const string ParamsKey = "params";

    public static TProblem WithCode<TProblem>(
        this TProblem problem,
        string code,
        IReadOnlyDictionary<string, object?>? parameters = null)
        where TProblem : ProblemDetails
    {
        problem.Extensions[CodeKey] = code;

        if (parameters is { Count: > 0 })
        {
            problem.Extensions[ParamsKey] = parameters;
        }

        return problem;
    }

    /// <summary>Applies the exception's own code when it carries one, otherwise <paramref name="fallbackCode"/>.</summary>
    public static TProblem WithCodeFrom<TProblem>(this TProblem problem, Exception exception, string? fallbackCode)
        where TProblem : ProblemDetails
    {
        if (exception is ICodedError coded)
        {
            return problem.WithCode(coded.Code, coded.Params);
        }

        return fallbackCode is null ? problem : problem.WithCode(fallbackCode);
    }
}
