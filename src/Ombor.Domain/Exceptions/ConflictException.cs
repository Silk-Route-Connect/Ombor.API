namespace Ombor.Domain.Exceptions;

/// <summary>
/// Raised when an operation is refused because it would violate a reference constraint —
/// e.g. deleting a category that still has products. Surfaces as HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception? innerException) : base(message, innerException) { }
}
