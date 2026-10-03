namespace Ombor.Domain.Exceptions;

/// <summary>
/// A coded authentication failure (bad credentials, deactivated account, dead session). Derives from
/// <see cref="UnauthorizedAccessException"/> so it keeps the existing 401 handling and Sentry exclusion.
/// </summary>
public sealed class AuthenticationFailedException : UnauthorizedAccessException, ICodedError
{
    public AuthenticationFailedException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public IReadOnlyDictionary<string, object?>? Params => null;
}
