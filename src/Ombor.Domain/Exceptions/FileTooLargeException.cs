namespace Ombor.Domain.Exceptions;

public sealed class FileTooLargeException : InvalidFileException
{
    public FileTooLargeException()
    {
    }

    public FileTooLargeException(string? message)
        : base(message)
    {
    }

    public FileTooLargeException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public FileTooLargeException(long actual, long max)
        : base($"File size {actual} exceeds max {max} bytes.")
    {
    }
}
