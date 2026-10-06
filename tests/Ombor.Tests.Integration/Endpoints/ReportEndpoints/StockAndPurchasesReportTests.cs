using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Contracts.Responses.Report;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ReportEndpoints;

public sealed class StockAndPurchasesReportTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ReportTestsBase(factory, output)
{
    [Fact]
    public async Task Purchases_ByProduct_ServesSuppliesNetOfReturns()
    {
        // 10 @ 50 with 10 % off = 450; 2 returned at the original net price = 90.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        var supply = await PostTransactionAsync(Document(TransactionType.Supply, partnerId, productId, warehouseId, 10m, 50m, discount: 10m));
        await PostTransactionAsync(Document(TransactionType.SupplyRefund, partnerId, productId, warehouseId, 2m, 50m, originalId: supply.Id));

        var report = await GetReportAsync<PurchasesReportDto>("purchases", $"{TodayOnly}&groupBy=Product");

        var row = Assert.Single(report.Rows, r => r.Key == productId.ToString());
        Assert.Equal(1, row.Documents);
        Assert.Equal(1, row.RefundDocuments);
        Assert.Equal(8m, row.Quantity);
        Assert.Equal(450m, row.Purchases);
        Assert.Equal(90m, row.Refunds);
        Assert.Equal(360m, row.NetPurchases);
        Assert.Equal(report.Totals.NetPurchases, report.Totals.Purchases - report.Totals.Refunds);
    }

    [Fact]
    public async Task Stock_ServesValueAtCostAndAtSalePrice_WithLowStockFlags_PerWarehouse()
    {
        var warehouseId = await CreateWarehouseAsync();
        var plenty = await CreateProductAsync(salePrice: 100m, lowStockThreshold: 10);
        var scarce = await CreateProductAsync(salePrice: 100m, lowStockThreshold: 10);
        await AddOpeningStockAsync(warehouseId, plenty, quantity: 20m, unitCost: 30m);
        await AddOpeningStockAsync(warehouseId, scarce, quantity: 5m, unitCost: 12m);

        var report = await GetReportAsync<StockReportDto>("stock", $"warehouseId={warehouseId}");

        Assert.Equal(2, report.Rows.Length);
        var plentyRow = Assert.Single(report.Rows, r => r.ProductId == plenty);
        Assert.Equal(20m, plentyRow.Quantity);
        Assert.Equal(30m, plentyRow.AverageCost);
        Assert.Equal(600m, plentyRow.Value);
        Assert.Equal(2_000m, plentyRow.SaleValue);
        Assert.False(plentyRow.IsLowStock);
        Assert.True(Assert.Single(report.Rows, r => r.ProductId == scarce).IsLowStock);

        var warehouse = Assert.Single(report.Warehouses);
        Assert.Equal(2, warehouse.ProductCount);
        Assert.Equal(660m, warehouse.Value);
        Assert.Equal(2_500m, warehouse.SaleValue);
        Assert.Equal(1, warehouse.LowStockCount);
        Assert.Equal(new StockReportTotalsDto(2, 660m, 2_500m, 1), report.Totals);
    }

    [Fact]
    public async Task Stock_KeepsArchivedWarehousesInTheTotals_AndAgreesWithTheDashboard()
    {
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 4m, unitCost: 25m);
        var warehouse = await _context.Warehouses.AsTracking().FirstAsync(w => w.Id == warehouseId);
        warehouse.IsArchived = true;
        await _context.SaveChangesAsync();

        var report = await GetReportAsync<StockReportDto>("stock", string.Empty);
        var dashboard = await _client.GetAsync<DashboardDto>("dashboard");

        var row = Assert.Single(report.Rows, r => r.WarehouseId == warehouseId);
        Assert.True(row.WarehouseIsArchived);
        Assert.Equal(100m, row.Value);
        Assert.True(Assert.Single(report.Warehouses, w => w.WarehouseId == warehouseId).IsArchived);
        Assert.Equal(dashboard.StockValue.Value, report.Totals.Value);
    }
}
