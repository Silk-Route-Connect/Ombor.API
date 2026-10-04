using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;
using Ombor.Tests.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.DashboardEndpoints;

public sealed class DashboardTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : DashboardTestsBase(factory, output)
{
    [Fact]
    public async Task Dashboard_DebtFigures_AreTheNetPartnerPositions_OfTheDebtSummary()
    {
        // Arrange — one partner with a fresh receivable, a 40-day receivable and a payable: it nets to +800.
        var partnerId = await CreatePartnerAsync();
        var now = DateTimeOffset.UtcNow;
        await AddOutstandingTransactionAsync(partnerId, TransactionType.Sale, 1_000m, 0m, now.AddDays(-40), BusinessDay.Today.AddDays(-30));
        await AddOutstandingTransactionAsync(partnerId, TransactionType.Sale, 500m, 0m, now, null);
        await AddOutstandingTransactionAsync(partnerId, TransactionType.Supply, 700m, 0m, now, null);

        // Act
        var summary = await GetDebtSummaryAsync();
        var dashboard = await GetDashboardAsync();

        // Assert — the same figures as GET /api/debts/summary (one source, live-ui-1).
        Assert.Equal(summary.Receivable, dashboard.Receivable.Value);
        Assert.Equal(summary.ReceivablePartnerCount, dashboard.Receivable.Count);
        Assert.Equal(summary.Payable, dashboard.Payable.Value);
        Assert.Equal(summary.PayablePartnerCount, dashboard.Payable.Count);
        Assert.Equal(summary.OlderThan30Days, dashboard.Overdue.Value);
        Assert.Equal(summary.Receivable, dashboard.Aging.Sum(a => a.Amount));

        // The partner nets to +800: the payable 700 settles the oldest item first, so 500 (new) + 300 (40 days) stay.
        var position = Assert.Single(summary.Partners, p => p.PartnerId == partnerId);
        Assert.Equal(800m, position.Balance);
        Assert.Equal(40, position.OldestAgeDays);

        // Top debtors are sorted desc, capped at 5, and each amount is the partner's net balance.
        Assert.True(dashboard.TopDebtors.Length <= 5);
        for (var i = 1; i < dashboard.TopDebtors.Length; i++)
        {
            Assert.True(dashboard.TopDebtors[i - 1].Amount >= dashboard.TopDebtors[i].Amount);
        }

        Assert.All(dashboard.TopDebtors, d => Assert.Equal(summary.Partners.Single(p => p.PartnerId == d.PartnerId).Balance, d.Amount));
    }

    [Theory]
    [InlineData("today")]
    [InlineData("week")]
    [InlineData("month")]
    public async Task Dashboard_EveryTrend_AlignsToTheSeries_AndEndsOnItsHeadline(string period)
    {
        var dashboard = await GetDashboardAsync(period);
        var buckets = dashboard.Series.Length;

        Assert.Equal(buckets, dashboard.Revenue.Trend.Length);
        Assert.Equal(buckets, dashboard.SaleRefunds.Trend.Length);
        Assert.Equal(buckets, dashboard.Receivable.Trend.Length);
        Assert.Equal(buckets, dashboard.Payable.Trend.Length);
        Assert.Equal(buckets, dashboard.Overdue.Trend.Length);
        Assert.Equal(buckets, dashboard.Cash.Trend.Length);

        // Positions end on today's value; flows add up to it.
        Assert.Equal(dashboard.Receivable.Value, dashboard.Receivable.Trend[^1]);
        Assert.Equal(dashboard.Payable.Value, dashboard.Payable.Trend[^1]);
        Assert.Equal(dashboard.Overdue.Value, dashboard.Overdue.Trend[^1]);
        Assert.Equal(dashboard.Cash.Value, dashboard.Cash.Trend[^1]);
        Assert.Equal(dashboard.Revenue.Value, dashboard.Revenue.Trend.Sum());
    }

    [Fact]
    public async Task Dashboard_ANewUnpaidSale_MovesTodaysReceivablePoint_NotYesterdays()
    {
        var partnerId = await CreatePartnerAsync();
        var before = await GetDashboardAsync("week");

        await AddOutstandingTransactionAsync(partnerId, TransactionType.Sale, 1_234m, 0m, DateTimeOffset.UtcNow, null);
        var after = await GetDashboardAsync("week");

        Assert.Equal(before.Receivable.Value + 1_234m, after.Receivable.Value);
        Assert.Equal(before.Receivable.Trend[^1] + 1_234m, after.Receivable.Trend[^1]);
        Assert.Equal(before.Receivable.Trend[^2], after.Receivable.Trend[^2]);
    }

    [Fact]
    public async Task Dashboard_BucketsFollowTheTashkentDay()
    {
        // A sale at 00:01 local time is 19:01 UTC the day before; it belongs to today's local bucket.
        var partnerId = await CreatePartnerAsync();
        var before = await GetDashboardAsync("week");

        await AddOutstandingTransactionAsync(
            partnerId, TransactionType.Sale, 4_321m, 0m, BusinessDay.StartOfDay(BusinessDay.Today).AddMinutes(1), null);
        var after = await GetDashboardAsync("week");

        Assert.Equal(BusinessDay.Today.ToString("yyyy-MM-dd"), after.Series[^1].Label);
        Assert.Equal(before.Series[^1].Sales + 4_321m, after.Series[^1].Sales);
        Assert.Equal(before.Series[^2].Sales, after.Series[^2].Sales);
    }

    [Fact]
    public async Task Dashboard_Revenue_IsNetOfSaleRefunds()
    {
        // Arrange — a 1 000 sale today, then a refund of one 300 line.
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 100, unitCost: 50m);
        var before = await GetDashboardAsync("today");

        var sale = await PostTransactionAsync(
            TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 300m, walletId: null, paidAmount: 0m));
        await PostTransactionAsync(TransactionRequestFactory.Refund(
            Contracts.Enums.TransactionType.SaleRefund, partnerId, productId, warehouseId, sale.Id, due: 300m));
        var after = await GetDashboardAsync("today");

        // Assert — the sale and its refund cancel out in revenue; the refund is shown on its own.
        Assert.Equal(before.Revenue.Value, after.Revenue.Value);
        Assert.Equal(before.Revenue.Count + 1, after.Revenue.Count);
        Assert.Equal(before.SaleRefunds.Value + 300m, after.SaleRefunds.Value);
        Assert.Equal(before.SaleRefunds.Count + 1, after.SaleRefunds.Count);
        Assert.Equal(after.SaleRefunds.Value, after.Series.Sum(s => s.SaleRefunds));
    }

    [Fact]
    public async Task Dashboard_CashAndStockValue_FollowWalletsAndStock()
    {
        var before = await GetDashboardAsync();

        var walletId = await CreateWalletAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10, unitCost: 40m);
        var after = await GetDashboardAsync();

        // A new wallet's opening balance is cash on hand; opening stock adds quantity × cost.
        Assert.Equal(before.Cash.Value + 100_000_000m, after.Cash.Value);
        Assert.Equal(100_000_000m, after.Cash.Wallets.Single(w => w.Id == walletId).Balance);
        Assert.Equal(after.Cash.Value, after.Cash.Wallets.Sum(w => w.Balance));
        Assert.Equal(before.StockValue.Value + 400m, after.StockValue.Value);
    }

    [Fact]
    public async Task Dashboard_RevenueAndPayin_ReconcileToTodayWindow()
    {
        // Arrange — a paid sale today feeds both revenue and an Income wallet component (pay-in).
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        var walletId = await CreateWalletAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 100, unitCost: 50m);
        await PostTransactionAsync(
            TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId, paidAmount: 1_000m));

        // Act
        var dashboard = await GetDashboardAsync("today");

        // Assert — revenue (sales net of refunds) and pay-in equal the same local-day aggregates from the DB.
        var (start, end) = TodayWindow();
        var expectedSales = await _context.Transactions
            .Where(t => t.Type == TransactionType.Sale && t.DateUtc >= start && t.DateUtc < end)
            .SumAsync(t => (decimal?)t.TotalDue) ?? 0m;
        var expectedRefunds = await _context.Transactions
            .Where(t => t.Type == TransactionType.SaleRefund && t.DateUtc >= start && t.DateUtc < end)
            .SumAsync(t => (decimal?)t.TotalDue) ?? 0m;
        var expectedPayin = await _context.PaymentComponents
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.WalletId != null
                && c.Payment.DateUtc >= start && c.Payment.DateUtc < end && c.Payment.Direction == PaymentDirection.Income)
            .SumAsync(c => (decimal?)c.Amount) ?? 0m;

        Assert.True(expectedSales >= 1_000m);
        Assert.Equal(expectedSales - expectedRefunds, dashboard.Revenue.Value);
        Assert.Equal(expectedSales, dashboard.Series.Sum(s => s.Sales));
        Assert.Equal(expectedPayin, dashboard.Series.Sum(s => s.Payin));
        // Per-wallet splits sum to the bucket totals.
        Assert.Equal(dashboard.Series.Sum(s => s.Payin), dashboard.Series.Sum(s => s.WalletPayin.Sum()));
    }

    [Theory]
    [InlineData("today", 24)]
    [InlineData("week", 7)]
    [InlineData("month", 30)]
    public async Task Dashboard_SeriesBucketCount_MatchesPeriod(string period, int expectedBuckets)
    {
        var dashboard = await GetDashboardAsync(period);

        Assert.Equal(expectedBuckets, dashboard.Series.Length);
        Assert.Equal(period, dashboard.Period);
    }

    [Fact]
    public async Task Dashboard_DefaultPeriod_IsMonth()
    {
        var dashboard = await GetDashboardAsync();

        Assert.Equal("month", dashboard.Period);
        Assert.Equal(30, dashboard.Series.Length);
    }

    [Fact]
    public async Task Dashboard_Wallets_ExposeThreeTypeValues_AndSeriesArraysAlign()
    {
        // Arrange
        var cash = await CreateWalletAsync(WalletType.Cash);
        var card = await CreateWalletAsync(WalletType.Card);
        var bank = await CreateWalletAsync(WalletType.Bank);

        // Act
        var dashboard = await GetDashboardAsync("today");

        // Assert — three distinct type values (kept, not collapsed to cash|bank).
        Assert.Equal("Cash", dashboard.Wallets.Single(w => w.Id == cash).Type);
        Assert.Equal("Card", dashboard.Wallets.Single(w => w.Id == card).Type);
        Assert.Equal("Bank", dashboard.Wallets.Single(w => w.Id == bank).Type);

        // The per-wallet series arrays align to wallets[].
        Assert.All(dashboard.Series, point =>
        {
            Assert.Equal(dashboard.Wallets.Length, point.WalletPayin.Length);
            Assert.Equal(dashboard.Wallets.Length, point.WalletPayout.Length);
        });
    }

    [Fact]
    public async Task Dashboard_RecentTransactions_IncludeNewestSaleAndSupply()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 100, unitCost: 50m);

        var sale = await PostTransactionAsync(
            TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId: null, paidAmount: 0m));
        var supply = await PostTransactionAsync(
            TransactionRequestFactory.Supply(partnerId, productId, warehouseId, due: 600m, walletId: null, paidAmount: 0m));

        // Act — both just-created transactions are the newest, so they head the (capped-10) list.
        var dashboard = await GetDashboardAsync();

        // Assert
        var saleRow = Assert.Single(dashboard.RecentTransactions, t => t.Id == sale.Id);
        Assert.Equal("Sale", saleRow.Type);
        Assert.Equal("unpaid", saleRow.Status);
        Assert.Equal(1_000m, saleRow.Total);
        Assert.Equal(0m, saleRow.Paid);

        var supplyRow = Assert.Single(dashboard.RecentTransactions, t => t.Id == supply.Id);
        Assert.Equal("Supply", supplyRow.Type);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) TodayWindow()
    {
        var dayStart = BusinessDay.StartOfDay(BusinessDay.Today);

        return (dayStart, dayStart.AddDays(1));
    }
}
