namespace Ombor.Domain.Exceptions;

public abstract class InvalidFileException : Exception
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
}
