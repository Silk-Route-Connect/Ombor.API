namespace Ombor.Contracts.Enums;

/// <summary>What a notification is about. Listed in the order the notifications are served.</summary>
public enum NotificationKind
{
    /// <summary>Unpaid sales past their due date.</summary>
    OverdueReceivables = 1,

    /// <summary>Open orders whose delivery date has passed.</summary>
    OrdersOverdue = 2,

    /// <summary>Active products at or below their low-stock threshold (the <c>ProductDto.isLowStock</c> rule).</summary>
    LowStock = 3,

    /// <summary>Open orders to deliver today.</summary>
    OrdersDueToday = 4,
}
