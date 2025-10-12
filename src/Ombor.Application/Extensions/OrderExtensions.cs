using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Extensions;

internal static class OrderExtensions
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Processing, OrderStatus.Cancelled, OrderStatus.Rejected],
        [OrderStatus.Processing] = [OrderStatus.Shipping, OrderStatus.Cancelled],
        [OrderStatus.Shipping] = [OrderStatus.Delivered, OrderStatus.Returned],
        [OrderStatus.Cancelled] = [],  // Terminal state
        [OrderStatus.Returned] = [],   // Terminal state
        [OrderStatus.Rejected] = []    // Terminal state
    };

    public static void ValidateTransition(this Order order, OrderStatus target)
    {
        ArgumentNullException.ThrowIfNull(order);

        var current = order.Status;

        if (!current.CanTransitionTo(target))
        {
            throw new InvalidOrderStateTransitionException(order.Id, current, target);
        }
    }

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus target) =>
        ValidTransitions.TryGetValue(current, out var allowed) && allowed.Contains(target);
}
