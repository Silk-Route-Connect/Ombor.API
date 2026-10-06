using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Report;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainTransactionStatus = Ombor.Domain.Enums.TransactionStatus;
using DomainTransactionType = Ombor.Domain.Enums.TransactionType;

namespace Ombor.Tests.Integration.Endpoints.ReportEndpoints;

public sealed class SalesReportTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ReportTestsBase(factory, output)
{
    [Fact]
    public async Task Sales_ByProduct_ServesRevenueAndTheSnapshottedCost_NetOfRefunds()
    {
        // 10 @ 40 opened; 3 sold @ 100 (cost 120); a supply @ 100 moves the WAC to 65; 1 returned at its own cost 40.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10m, unitCost: 40m);
        var sale = await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, 3m, 100m));
        await PostTransactionAsync(Document(TransactionType.Supply, partnerId, productId, warehouseId, 5m, 100m));
        await PostTransactionAsync(Document(TransactionType.SaleRefund, partnerId, productId, warehouseId, 1m, 100m, originalId: sale.Id));

        var report = await GetReportAsync<SalesReportDto>("sales", $"{TodayOnly}&groupBy=Product");

        Assert.Equal(ReportGroupBy.Product, report.GroupBy);
        var row = Assert.Single(report.Rows, r => r.Key == productId.ToString());
        Assert.StartsWith("Product ", row.Label);
        Assert.Equal(1, row.Documents);
        Assert.Equal(1, row.RefundDocuments);
        Assert.Equal(2m, row.Quantity);
        Assert.Equal(300m, row.Revenue);
        Assert.Equal(100m, row.Refunds);
        Assert.Equal(200m, row.NetRevenue);
        Assert.Equal(80m, row.Cost);
        Assert.Equal(120m, row.GrossProfit);
        Assert.Equal(60m, row.MarginPercent);
        Assert.False(row.CostIsEstimated);
    }

    [Fact]
    public async Task Sales_PartnerRow_ReconcilesWithTheTransactionsList_AndTotalsMatchAcrossGroupings()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 20m, unitCost: 10m);
        var first = await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, 2m, 150m, discount: 10m));
        await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, 3m, 33.33m, discount: 3m));
        await PostTransactionAsync(Document(TransactionType.SaleRefund, partnerId, productId, warehouseId, 1m, 150m, originalId: first.Id));

        var byPartner = await GetReportAsync<SalesReportDto>("sales", $"{TodayOnly}&groupBy=Partner");
        var documents = await _client.GetAsync<TransactionDto[]>($"transactions?partnerId={partnerId}");

        var row = Assert.Single(byPartner.Rows, r => r.Key == partnerId.ToString());
        var sales = documents.Where(d => d.Type == "Sale").ToArray();
        var refunds = documents.Where(d => d.Type == "SaleRefund").ToArray();
        Assert.Equal(sales.Length, row.Documents);
        Assert.Equal(refunds.Length, row.RefundDocuments);
        Assert.Equal(sales.Sum(d => d.TotalDue), row.Revenue);
        Assert.Equal(refunds.Sum(d => d.TotalDue), row.Refunds);

        // Every grouping splits the same lines, so the period totals are identical.
        foreach (var groupBy in new[] { "Day", "Week", "Month", "Product", "Category", "Warehouse" })
        {
            var other = await GetReportAsync<SalesReportDto>("sales", $"{TodayOnly}&groupBy={groupBy}");
            Assert.Equal(byPartner.Totals, other.Totals);
        }
    }

    [Fact]
    public async Task Sales_TimeGroupings_ListEveryBucketOfThePeriod()
    {
        var byDay = await GetReportAsync<SalesReportDto>("sales", $"from={Day(Today.AddDays(-6))}&to={Day(Today)}&groupBy=Day");
        Assert.Equal(7, byDay.Rows.Length);
        Assert.Equal(Day(Today.AddDays(-6)), byDay.Rows[0].Key);
        Assert.Equal(Day(Today), byDay.Rows[^1].Key);
        Assert.Equal(byDay.Totals.Revenue, byDay.Rows.Sum(r => r.Revenue));

        // 1 September 2026 is a Tuesday: its week is keyed by Monday 31 August.
        var byWeek = await GetReportAsync<SalesReportDto>("sales", "from=2026-09-01&to=2026-09-30&groupBy=Week");
        Assert.Equal(new[] { "2026-08-31", "2026-09-07", "2026-09-14", "2026-09-21", "2026-09-28" }, byWeek.Rows.Select(r => r.Key));

        var byMonth = await GetReportAsync<SalesReportDto>("sales", "from=2026-01-15&to=2026-03-02&groupBy=Month");
        Assert.Equal(new[] { "2026-01", "2026-02", "2026-03" }, byMonth.Rows.Select(r => r.Key));
        Assert.Equal(new DateOnly(2026, 1, 15), byMonth.From);
        Assert.Equal(new DateOnly(2026, 3, 2), byMonth.To);
    }

    [Fact]
    public async Task Sales_WithoutAPeriod_CoversTheCurrentMonthByDay()
    {
        var report = await _client.GetAsync<SalesReportDto>($"{Reports}/sales");

        Assert.Equal(new DateOnly(Today.Year, Today.Month, 1), report.From);
        Assert.Equal(Today, report.To);
        Assert.Equal(ReportGroupBy.Day, report.GroupBy);
        Assert.Equal(Today.Day, report.Rows.Length);
    }

    [Fact]
    public async Task Sales_AnEstimatedCost_IsFlaggedOnItsRowAndTheReport()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();

        // A sale recorded before cost snapshots, as the backfill leaves it.
        _context.Transactions.Add(new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            WarehouseId = warehouseId,
            Type = DomainTransactionType.Sale,
            Status = DomainTransactionStatus.Open,
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = 500m,
            Lines =
            [
                new TransactionLine
                {
                    ProductId = productId,
                    Quantity = 5m,
                    UnitPrice = 100m,
                    UnitCost = 60m,
                    CostIsEstimated = true,
                    Product = null!,
                    Transaction = null!,
                },
            ],
        });
        await _context.SaveChangesAsync();

        var report = await GetReportAsync<SalesReportDto>("sales", $"{TodayOnly}&groupBy=Product");

        var row = Assert.Single(report.Rows, r => r.Key == productId.ToString());
        Assert.Equal(300m, row.Cost);
        Assert.Equal(200m, row.GrossProfit);
        Assert.True(row.CostIsEstimated);
        Assert.True(report.CostIsEstimated);
    }
}
