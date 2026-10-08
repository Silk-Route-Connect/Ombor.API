using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Notification;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Helpers;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainEnums = Ombor.Domain.Enums;

namespace Ombor.Tests.Integration.Endpoints.NotificationEndpoints;

/// <summary>
/// GET /api/notifications — the money and order alerts, and the alert list as a whole. Low stock has its own tests
/// (<see cref="LowStockNotificationTests"/>).
/// </summary>
public sealed class NotificationTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : NotificationTestsBase(factory, output)
{
    [Fact]
    public async Task OverdueReceivables_CountUnpaidSalesPastTheirDueDate_WithWhatIsLeftToPay()
    {
        var before = await GetAsync(NotificationKind.OverdueReceivables);
        var partnerId = await AddPartnerAsync(_context);
        var today = BusinessDay.Today;

        // Counted: past due and part-paid (the oldest due date, so it heads the list).
        var overdue = await AddTransactionAsync(_context, partnerId, DomainEnums.TransactionType.Sale, 5_000m, 1_200m, today.AddDays(-4_000));

        // Not counted: due today, fully paid, a supply (we owe that), and another organization's overdue sale.
        await AddTransactionAsync(_context, partnerId, DomainEnums.TransactionType.Sale, 900m, 0m, today);
        await AddTransactionAsync(_context, partnerId, DomainEnums.TransactionType.Sale, 900m, 900m, today.AddDays(-30));
        await AddTransactionAsync(_context, partnerId, DomainEnums.TransactionType.Supply, 900m, 0m, today.AddDays(-30));
        await using (var foreign = CreateContext(ForeignOrganizationId))
        {
            await AddTransactionAsync(foreign, await AddPartnerAsync(foreign), DomainEnums.TransactionType.Sale, 900m, 0m, today.AddDays(-30));
        }

        var after = await GetAsync(NotificationKind.OverdueReceivables);

        Assert.NotNull(after);
        Assert.Equal(NotificationSeverity.Warning, after.Severity);
        Assert.Equal((before?.Count ?? 0) + 1, after.Count);
        Assert.Equal((before?.Amount ?? 0m) + 3_800m, after.Amount);
        var item = after.Items[0];
        Assert.Equal(overdue, item.Id);
        Assert.Equal(ActivityEntityKind.Sale, item.EntityKind);
        Assert.Equal(3_800m, item.Amount);
        Assert.Equal(4_000, item.Days);
        Assert.Equal(today.AddDays(-4_000), item.Date);
        Assert.StartsWith("Partner", item.Detail);
    }

    [Fact]
    public async Task Orders_LateAndDueToday_AreSeparateAlerts_OpenOrdersOnly()
    {
        var beforeLate = await GetAsync(NotificationKind.OrdersOverdue);
        var beforeToday = await GetAsync(NotificationKind.OrdersDueToday);
        var customerId = await AddPartnerAsync(_context);
        var today = BusinessDay.Today;

        var late = await AddOrderAsync(customerId, DomainEnums.OrderStatus.Processing, today.AddDays(-3_000));
        var dueToday = await AddOrderAsync(customerId, DomainEnums.OrderStatus.Pending, today);
        await AddOrderAsync(customerId, DomainEnums.OrderStatus.Cancelled, today.AddDays(-2));
        await AddOrderAsync(customerId, DomainEnums.OrderStatus.Delivered, today);
        await AddOrderAsync(customerId, DomainEnums.OrderStatus.Pending, today.AddDays(1));
        await AddOrderAsync(customerId, DomainEnums.OrderStatus.Pending, null);

        var afterLate = await GetAsync(NotificationKind.OrdersOverdue);
        var afterToday = await GetAsync(NotificationKind.OrdersDueToday);

        Assert.NotNull(afterLate);
        Assert.NotNull(afterToday);
        Assert.Equal((beforeLate?.Count ?? 0) + 1, afterLate.Count);
        Assert.Equal((beforeToday?.Count ?? 0) + 1, afterToday.Count);
        Assert.Equal(NotificationSeverity.Warning, afterLate.Severity);
        Assert.Equal(NotificationSeverity.Info, afterToday.Severity);
        Assert.Equal(late, afterLate.Items[0].Id);
        Assert.Equal(3_000, afterLate.Items[0].Days);
        Assert.Equal(ActivityEntityKind.Order, afterLate.Items[0].EntityKind);
        var todayItem = Assert.Single(afterToday.Items, i => i.Id == dueToday);
        Assert.Equal(0, todayItem.Days);
        Assert.Equal(2_000m, todayItem.Amount);
    }

    [Fact]
    public async Task Notifications_ServeOnlyAlertsWithSomethingToReport_InKindOrder()
    {
        var partnerId = await AddPartnerAsync(_context);
        await AddTransactionAsync(_context, partnerId, DomainEnums.TransactionType.Sale, 100m, 0m, BusinessDay.Today.AddDays(-1));

        var all = await _client.GetAsync<NotificationDto[]>(Route);

        Assert.NotEmpty(all);
        Assert.All(all, n => Assert.True(n.Count > 0 && n.Items.Length is > 0 and <= 10));
        Assert.Equal(all.Select(n => n.Kind).Order(), all.Select(n => n.Kind));
        Assert.Equal(all.Length, all.Select(n => n.Kind).Distinct().Count());
    }

    private static async Task<int> AddPartnerAsync(IApplicationDbContext context)
    {
        var partner = new Partner { Name = $"Partner {Guid.NewGuid():N}", Type = DomainEnums.PartnerType.Customer };
        context.Partners.Add(partner);
        await context.SaveChangesAsync();

        return partner.Id;
    }

    private static async Task<int> AddTransactionAsync(
        IApplicationDbContext context, int partnerId, DomainEnums.TransactionType type, decimal totalDue, decimal totalPaid, DateOnly dueDate)
    {
        var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent" };
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            WarehouseId = warehouse.Id,
            Type = type,
            Status = TransactionRecord.SettlementStatusOf(totalDue, totalPaid),
            DateUtc = DateTimeOffset.UtcNow.AddDays(-4_100),
            DueDate = dueDate,
            TotalDue = totalDue,
            TotalPaid = totalPaid,
        };
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        return transaction.Id;
    }

    private async Task<int> AddOrderAsync(int customerId, DomainEnums.OrderStatus status, DateOnly? deliveryDate)
    {
        var order = new Order
        {
            CustomerId = customerId,
            Customer = null!,
            TotalAmount = 2_000m,
            DateUtc = DateTimeOffset.UtcNow,
            Status = status,
            DeliveryDate = deliveryDate,
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order.Id;
    }
}
