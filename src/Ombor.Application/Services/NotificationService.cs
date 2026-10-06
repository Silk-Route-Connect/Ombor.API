using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Notification;
using OrderStatus = Ombor.Domain.Enums.OrderStatus;
using TransactionStatus = Ombor.Domain.Enums.TransactionStatus;
using TransactionType = Ombor.Domain.Enums.TransactionType;

namespace Ombor.Application.Services;

/// <summary>
/// The bell: alerts computed from the ledger on every read — nothing is stored, so an alert disappears as soon as its
/// cause is fixed (the sale paid, the order delivered, the stock refilled). Overdue sales and low stock cost a count
/// and a top-10 query each; due orders are one narrow query (open orders due by today are few).
/// </summary>
internal sealed class NotificationService(IApplicationDbContext context, IBusinessClock clock) : INotificationService
{
    private const int MaxItems = 10;

    private static readonly OrderStatus[] OpenOrderStatuses = [OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Shipping];

    public async Task<NotificationDto[]> GetAsync()
    {
        var today = clock.Today;

        NotificationDto?[] notifications =
        [
            await OverdueReceivablesAsync(today),
            .. await OrdersDueAsync(today),
            await LowStockAsync(),
        ];

        return [.. notifications.OfType<NotificationDto>().OrderBy(n => n.Kind)];
    }

    /// <summary>Unpaid sales past their due date — the same rule as a document's served «Overdue» status.</summary>
    private async Task<NotificationDto?> OverdueReceivablesAsync(DateOnly today)
    {
        var overdue = context.Transactions
            .AsNoTracking()
            .Where(t => t.Type == TransactionType.Sale
                && t.Status != TransactionStatus.Closed
                && t.TotalDue > t.TotalPaid
                && t.DueDate != null
                && t.DueDate < today);

        var count = await overdue.CountAsync();

        if (count == 0)
        {
            return null;
        }

        var amount = await overdue.SumAsync(t => t.TotalDue - t.TotalPaid);
        var top = await overdue
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.TotalDue - t.TotalPaid)
            .ThenBy(t => t.Id)
            .Take(MaxItems)
            .Select(t => new { t.Id, t.Number, t.DueDate, Remaining = t.TotalDue - t.TotalPaid, Partner = t.Partner.Name })
            .ToArrayAsync();

        return new NotificationDto(
            NotificationKind.OverdueReceivables,
            NotificationSeverity.Warning,
            count,
            amount,
            [.. top.Select(t => new NotificationItemDto(
                ActivityEntityKind.Sale,
                t.Id,
                t.Number?.ToString(CultureInfo.InvariantCulture),
                t.Partner,
                Amount: t.Remaining,
                Date: t.DueDate,
                Days: today.DayNumber - t.DueDate!.Value.DayNumber))]);
    }

    /// <summary>Open orders (pending, processing, shipping) whose delivery date has passed, and those due today.</summary>
    private async Task<NotificationDto[]> OrdersDueAsync(DateOnly today)
    {
        var due = await context.Orders
            .AsNoTracking()
            .Where(o => OpenOrderStatuses.Contains(o.Status) && o.DeliveryDate != null && o.DeliveryDate <= today)
            .Select(o => new DueOrder(o.Id, o.OrderNumber, o.DeliveryDate!.Value, o.DeliveryTime, o.TotalAmount, o.Customer.Name))
            .ToArrayAsync();

        var overdue = due
            .Where(o => o.DeliveryDate < today)
            .OrderBy(o => o.DeliveryDate)
            .ThenBy(o => o.Id);
        var dueToday = due
            .Where(o => o.DeliveryDate == today)
            .OrderBy(o => o.DeliveryTime ?? TimeOnly.MaxValue)
            .ThenBy(o => o.Id);

        NotificationDto?[] notifications =
        [
            OrdersNotification(NotificationKind.OrdersOverdue, NotificationSeverity.Warning, [.. overdue], today),
            OrdersNotification(NotificationKind.OrdersDueToday, NotificationSeverity.Info, [.. dueToday], today),
        ];

        return [.. notifications.OfType<NotificationDto>()];
    }

    /// <summary>
    /// Active products whose stock over all warehouses is at or below their threshold — the <c>ProductDto.isLowStock</c>
    /// rule; the largest shortage first. Archived products are left out: nobody restocks them.
    /// </summary>
    private async Task<NotificationDto?> LowStockAsync()
    {
        var low = context.Products
            .AsNoTracking()
            .Where(p => !p.IsArchived)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.SKU,
                p.Measurement,
                p.LowStockThreshold,
                Stock = p.WarehouseItems.Sum(i => i.Quantity),
            })
            .Where(p => p.Stock <= p.LowStockThreshold);

        var count = await low.CountAsync();

        if (count == 0)
        {
            return null;
        }

        var top = await low
            .OrderByDescending(p => p.LowStockThreshold - p.Stock)
            .ThenBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Take(MaxItems)
            .ToArrayAsync();

        return new NotificationDto(
            NotificationKind.LowStock,
            NotificationSeverity.Warning,
            count,
            null,
            [.. top.Select(p => new NotificationItemDto(
                ActivityEntityKind.Product,
                p.Id,
                p.Name,
                p.SKU,
                Quantity: p.Stock,
                Threshold: p.LowStockThreshold,
                Measurement: p.Measurement.ToString()))]);
    }

    private static NotificationDto? OrdersNotification(
        NotificationKind kind, NotificationSeverity severity, DueOrder[] orders, DateOnly today) =>
        orders.Length == 0
            ? null
            : new NotificationDto(
                kind,
                severity,
                orders.Length,
                null,
                [.. orders.Take(MaxItems).Select(o => new NotificationItemDto(
                    ActivityEntityKind.Order,
                    o.Id,
                    o.Number?.ToString(CultureInfo.InvariantCulture),
                    o.Customer,
                    Amount: o.Total,
                    Date: o.DeliveryDate,
                    Days: today.DayNumber - o.DeliveryDate.DayNumber))]);

    private sealed record DueOrder(int Id, int? Number, DateOnly DeliveryDate, TimeOnly? DeliveryTime, decimal Total, string Customer);
}
