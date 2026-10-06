namespace Ombor.Domain.Exceptions;

/// <summary>An upload exceeds the size limit (400, <see cref="ErrorCodes.FileTooLarge"/>, param <c>maxMegabytes</c>).</summary>
public sealed class FileTooLargeException : InvalidFileException
{
    private const long BytesPerMegabyte = 1024 * 1024;

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
        MaxBytes = max;
    }

    public long? MaxBytes { get; }

    public override string Code => ErrorCodes.FileTooLarge;

    public override IReadOnlyDictionary<string, object?>? Params => MaxBytes is long max
        ? new Dictionary<string, object?> { ["maxMegabytes"] = Math.Max(1, max / BytesPerMegabyte) }
        : null;
}
