namespace Ombor.Domain.Exceptions;

/// <summary>
/// An upload is empty, has no name, or its bytes are not the type its extension claims (or not a type this upload
/// accepts at all) — checked by content, never by the client's <c>Content-Type</c>.
/// </summary>
public sealed class InvalidFileContentException : InvalidFileException
{
    public InvalidFileContentException()
    {
    }

    public InvalidFileContentException(string? message)
        : base(message)
    {
    }

    public InvalidFileContentException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
