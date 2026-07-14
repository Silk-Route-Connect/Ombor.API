using Ombor.Application.Extensions;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Extensions;

public sealed class OrderExtensionsTests
{
    [Theory]
    // Pending can go to Processing, Cancelled, or Rejected.
    [InlineData(OrderStatus.Pending, OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Rejected, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipping, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered, false)]
    // Processing can go to Shipping or Cancelled.
    [InlineData(OrderStatus.Processing, OrderStatus.Shipping, true)]
    [InlineData(OrderStatus.Processing, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Processing, OrderStatus.Rejected, false)]
    // Shipping can only go to Delivered (Returned is now reached after delivery, not from shipping).
    [InlineData(OrderStatus.Shipping, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Shipping, OrderStatus.Returned, false)]
    [InlineData(OrderStatus.Shipping, OrderStatus.Cancelled, false)]
    // Delivered can be returned.
    [InlineData(OrderStatus.Delivered, OrderStatus.Returned, true)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled, false)]
    // Terminal states allow nothing.
    [InlineData(OrderStatus.Cancelled, OrderStatus.Processing, false)]
    [InlineData(OrderStatus.Returned, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Rejected, OrderStatus.Processing, false)]
    public void CanTransitionTo_ShouldReflectTheStateMachine(OrderStatus from, OrderStatus to, bool expected)
        => Assert.Equal(expected, from.CanTransitionTo(to));

    [Fact]
    public void ValidateTransition_ShouldThrow_WhenTransitionIsIllegal()
    {
        var order = NewOrder(OrderStatus.Pending);

        Assert.Throws<InvalidOrderStateTransitionException>(() => order.ValidateTransition(OrderStatus.Delivered));
    }

    [Fact]
    public void ValidateTransition_ShouldNotThrow_WhenTransitionIsLegal()
    {
        var order = NewOrder(OrderStatus.Shipping);

        order.ValidateTransition(OrderStatus.Delivered);
    }

    private static Order NewOrder(OrderStatus status) => new()
    {
        OrderNumber = 1,
        TotalAmount = 0m,
        DateUtc = DateTimeOffset.UtcNow,
        CustomerId = 1,
        Customer = null!,
        Status = status,
    };
}
