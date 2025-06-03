namespace Ombor.Domain.Exceptions;

public sealed class UnsupportedFileFormatException : InvalidFileException
{
    public string Extension { get; }
    public string[] Allowed { get; }

    public UnsupportedFileFormatException()
    {
        Extension = string.Empty;
        Allowed = [];
    }

    public UnsupportedFileFormatException(string? message)
        : base(message)
    {
        Extension = string.Empty;
        Allowed = [];
    }

    public UnsupportedFileFormatException(string? message, Exception? innerException)
        : base(message, innerException)
    {
        Extension = string.Empty;
        Allowed = [];
    }

    public UnsupportedFileFormatException(string extension, IEnumerable<string> allowed)
        : base($"Extension '{extension}' is not permitted. Allowed extensions: {string.Join(", ", allowed)}")
    {
        Extension = extension;
        Allowed = [.. allowed];
    }
}
