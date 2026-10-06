namespace Ombor.Domain.Exceptions;

/// <summary>
/// Raised when an operation is refused because it conflicts with the current state — e.g. deleting a category that
/// still has products (<see cref="ErrorCodes.EntityReferenced"/>), or a write that could not get its organization's
/// write lock in time (<see cref="ErrorCodes.ConflictBusy"/>). Surfaces as HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception, ICodedError
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception? innerException) : base(message, innerException) { }

    public ConflictException(string message, string code, Exception? innerException = null) : base(message, innerException)
        => Code = code;

    public string Code { get; } = ErrorCodes.EntityReferenced;

    public IReadOnlyDictionary<string, object?>? Params => null;
}
