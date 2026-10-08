namespace Ombor.Domain.Exceptions;

/// <summary>
/// An upload was rejected (empty, too large, not an allowed type, or content that does not match its extension).
/// Surfaces as a 400 with <see cref="ErrorCodes.FileInvalid"/> unless a subtype names a more specific code.
/// </summary>
public abstract class InvalidFileException : Exception, ICodedError
{
    protected InvalidFileException() : base()
    {
    }

    protected InvalidFileException(string? message) : base(message)
    {
    }

    protected InvalidFileException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public virtual string Code => ErrorCodes.FileInvalid;

    public virtual IReadOnlyDictionary<string, object?>? Params => null;
}
