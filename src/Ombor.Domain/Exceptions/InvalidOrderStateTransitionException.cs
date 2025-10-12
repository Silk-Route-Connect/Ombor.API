using Ombor.Domain.Enums;

namespace Ombor.Domain.Exceptions;

public sealed class InvalidOrderStateTransitionException : Exception
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

    }
}
