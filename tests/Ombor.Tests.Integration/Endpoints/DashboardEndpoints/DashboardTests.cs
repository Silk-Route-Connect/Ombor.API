using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.DashboardEndpoints;

public sealed class DashboardTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : DashboardTestsBase(factory, output)
{
    [Fact]
    public async Task Dashboard_DebtFigures_ReconcileWithDebtsEndpoint()
    {
        // Arrange — a fresh receivable, an overdue (40-day) receivable, and a payable.
        var partnerId = await CreatePartnerAsync();
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        await AddOutstandingTransactionAsync(partnerId, TransactionType.Sale, 1_000m, 0m, now.AddDays(-40), today.AddDays(-30));
        await AddOutstandingTransactionAsync(partnerId, TransactionType.Sale, 500m, 0m, now, null);
        await AddOutstandingTransactionAsync(partnerId, TransactionType.Supply, 700m, 0m, now, null);

        // Act
        var debts = await GetDebtsAsync();
        var dashboard = await GetDashboardAsync();

        // Assert — every debt figure equals the matching aggregate over the /debts list (same source, §K).
        var receivable = debts.Where(d => d.Direction == "Receivable").ToArray();
        var payable = debts.Where(d => d.Direction == "Payable").ToArray();
        var overdue = receivable.Where(d => d.AgeDays >= 31).ToArray();

        Assert.Equal(receivable.Sum(d => d.Remaining), dashboard.Receivable.Value);
        Assert.Equal(receivable.Length, dashboard.Receivable.Count);
        Assert.Null(dashboard.Receivable.DeltaPct); // snapshot, no period basis

        Assert.Equal(payable.Sum(d => d.Remaining), dashboard.Payable.Value);
        Assert.Equal(overdue.Sum(d => d.Remaining), dashboard.Overdue.Value);
        Assert.Equal(overdue.Select(d => d.PartnerId).Distinct().Count(), dashboard.Overdue.PartnerCount);

        // Aging buckets partition all receivables.
        Assert.Equal(receivable.Sum(d => d.Remaining), dashboard.Aging.Sum(a => a.Amount));

        // Top debtors are sorted desc, capped at 5, and each amount reconciles to /debts.
        Assert.True(dashboard.TopDebtors.Length <= 5);
        for (var i = 1; i < dashboard.TopDebtors.Length; i++)
        {
            Assert.True(dashboard.TopDebtors[i - 1].Amount >= dashboard.TopDebtors[i].Amount);
        }

        foreach (var debtor in dashboard.TopDebtors)
        {
            var expected = receivable.Where(d => d.PartnerId == debtor.PartnerId).Sum(d => d.Remaining);
            Assert.Equal(expected, debtor.Amount);
        }
    }

    [Fact]
    public async Task Dashboard_RevenueAndPayin_ReconcileToTodayWindow()
    {
        // Arrange — a paid sale today feeds both gross revenue and an Income wallet component (pay-in).
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        var walletId = await CreateWalletAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 100, unitCost: 50m);
        await PostTransactionAsync(
            TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId, paidAmount: 1_000m));

        // Act
        var dashboard = await GetDashboardAsync("today");

        // Assert — revenue/pay-in equal the same-window aggregates from the DB, and the series sums match.
        var (start, end) = TodayWindowUtc();
        var expectedRevenue = await _context.Transactions
            .Where(t => t.Type == TransactionType.Sale && t.DateUtc >= start && t.DateUtc < end)
            .SumAsync(t => (decimal?)t.TotalDue) ?? 0m;
        var expectedPayin = await _context.PaymentComponents
            .Where(c => c.SourceType == PaymentSourceType.Wallet && c.WalletId != null
                && c.Payment.DateUtc >= start && c.Payment.DateUtc < end && c.Payment.Direction == PaymentDirection.Income)
            .SumAsync(c => (decimal?)c.Amount) ?? 0m;

        Assert.True(expectedRevenue >= 1_000m);
        Assert.Equal(expectedRevenue, dashboard.Revenue.Value);
        Assert.Equal(expectedRevenue, dashboard.Series.Sum(s => s.Sales));
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

    private static (DateTimeOffset Start, DateTimeOffset End) TodayWindowUtc()
    {
        var dayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        return (dayStart, dayStart.AddDays(1));
    }
}
