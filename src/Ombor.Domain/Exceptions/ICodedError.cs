namespace Ombor.Domain.Exceptions;

/// <summary>
/// An exception that carries a machine-readable <see cref="ErrorCodes">error code</see> (and optional parameters).
/// The API's exception handlers serve them as the <c>code</c> / <c>params</c> extension members of the
/// ProblemDetails body.
/// </summary>
public interface ICodedError
{
    /// <summary>One of <see cref="ErrorCodes"/>.</summary>
    string Code { get; }

    /// <summary>Values the client interpolates into the localized message (camelCase keys); null when none.</summary>
    IReadOnlyDictionary<string, object?>? Params { get; }
}
