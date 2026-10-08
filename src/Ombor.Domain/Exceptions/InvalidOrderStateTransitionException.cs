using Ombor.Domain.Enums;

namespace Ombor.Domain.Exceptions;

/// <summary>
/// An order status change the order's state machine does not allow (e.g. delivering a cancelled order). Surfaces as
/// HTTP 409 with <see cref="ErrorCodes.OrderInvalidTransition"/>; <c>params.from</c> / <c>params.to</c> name the
/// statuses when known.
/// </summary>
public sealed class InvalidOrderStateTransitionException : Exception, ICodedError
{
    public InvalidOrderStateTransitionException()
    {
    }

    public InvalidOrderStateTransitionException(string? message) : base(message)
    {
    }

    public InvalidOrderStateTransitionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public InvalidOrderStateTransitionException(int orderId, OrderStatus current, OrderStatus target)
        : base($"Invalid state transition request for order: {orderId}. Transitioning from state: {current} to {target} is not allowed.")
    {
        Params = new Dictionary<string, object?> { ["from"] = current.ToString(), ["to"] = target.ToString() };
    }

    public string Code => ErrorCodes.OrderInvalidTransition;

    public IReadOnlyDictionary<string, object?>? Params { get; }
}
