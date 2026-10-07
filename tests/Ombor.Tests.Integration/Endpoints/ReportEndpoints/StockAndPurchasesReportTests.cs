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
        var plenty = await CreateProductAsync(salePrice: 100m);
        var scarce = await CreateProductAsync(salePrice: 100m);
        await AddOpeningStockAsync(warehouseId, plenty, quantity: 20m, unitCost: 30m);
        await AddOpeningStockAsync(warehouseId, scarce, quantity: 5m, unitCost: 12m);
        await SetLowStockThresholdAsync(warehouseId, plenty, 10m);
        await SetLowStockThresholdAsync(warehouseId, scarce, 10m);

        var report = await GetReportAsync<StockReportDto>("stock", $"warehouseId={warehouseId}");

        Assert.Equal(2, report.Rows.Length);
        var plentyRow = Assert.Single(report.Rows, r => r.ProductId == plenty);
        Assert.Equal(20m, plentyRow.Quantity);
        Assert.Equal(30m, plentyRow.AverageCost);
        Assert.Equal(600m, plentyRow.Value);
        Assert.Equal(2_000m, plentyRow.SaleValue);
        Assert.Equal(10m, plentyRow.LowStockThreshold);
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
    public async Task Stock_CountsOnlyTrackedRowsAtOrBelowTheirThreshold_EmptyOnesIncluded()
    {
        var warehouseId = await CreateWarehouseAsync();
        var above = await StockAsync(warehouseId, quantity: 20m, threshold: 10m);
        var below = await StockAsync(warehouseId, quantity: 5m, threshold: 10m);
        var atThreshold = await StockAsync(warehouseId, quantity: 10m, threshold: 10m);
        var trackedEmpty = await StockAsync(warehouseId, quantity: 4m, threshold: 3m, sellOut: true);
        var untrackedEmpty = await StockAsync(warehouseId, quantity: 4m, threshold: null, sellOut: true);
        var untrackedScarce = await StockAsync(warehouseId, quantity: 1m, threshold: null);

        var report = await GetReportAsync<StockReportDto>("stock", $"warehouseId={warehouseId}");

        Assert.Equal(6, report.Rows.Length);
        Assert.False(Row(report, above).IsLowStock);
        Assert.True(Row(report, below).IsLowStock);
        Assert.True(Row(report, atThreshold).IsLowStock);
        Assert.Equal(0m, Row(report, trackedEmpty).Quantity);
        Assert.True(Row(report, trackedEmpty).IsLowStock);

        // Not tracked: never low, whatever the quantity — not even at zero.
        Assert.Null(Row(report, untrackedEmpty).LowStockThreshold);
        Assert.Equal(0m, Row(report, untrackedEmpty).Quantity);
        Assert.False(Row(report, untrackedEmpty).IsLowStock);
        Assert.Null(Row(report, untrackedScarce).LowStockThreshold);
        Assert.False(Row(report, untrackedScarce).IsLowStock);

        Assert.Equal(3, Assert.Single(report.Warehouses).LowStockCount);
        Assert.Equal(3, report.Totals.LowStockCount);
        Assert.Equal(4, report.Totals.ProductCount);
    }

    [Fact]
    public async Task Stock_LeavesArchivedProductsAndWarehousesOutOfTheLowStockCounts()
    {
        var warehouseId = await CreateWarehouseAsync();
        var active = await StockAsync(warehouseId, quantity: 2m, threshold: 10m);
        var archivedProduct = await StockAsync(warehouseId, quantity: 2m, threshold: 10m, archivedProduct: true);
        var archivedWarehouseId = await CreateWarehouseAsync(archived: true);
        var inArchivedWarehouse = await StockAsync(archivedWarehouseId, quantity: 2m, threshold: 10m);

        var report = await GetReportAsync<StockReportDto>("stock", $"warehouseId={warehouseId}");
        var archived = await GetReportAsync<StockReportDto>("stock", $"warehouseId={archivedWarehouseId}");

        Assert.True(Row(report, active).IsLowStock);
        var archivedRow = Row(report, archivedProduct);
        Assert.True(archivedRow.ProductIsArchived);
        Assert.Equal(10m, archivedRow.LowStockThreshold);
        Assert.False(archivedRow.IsLowStock);
        Assert.Equal(1, report.Totals.LowStockCount);

        var archivedWarehouseRow = Row(archived, inArchivedWarehouse);
        Assert.True(archivedWarehouseRow.WarehouseIsArchived);
        Assert.False(archivedWarehouseRow.IsLowStock);
        Assert.Equal(0, Assert.Single(archived.Warehouses).LowStockCount);
        Assert.Equal(0, archived.Totals.LowStockCount);
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

    private static StockReportRowDto Row(StockReportDto report, int productId) =>
        Assert.Single(report.Rows, r => r.ProductId == productId);

    /// <summary>A fresh product stocked through opening stock, then given its threshold; sold out by a write-off when asked.</summary>
    private async Task<int> StockAsync(
        int warehouseId, decimal quantity, decimal? threshold, bool sellOut = false, bool archivedProduct = false)
    {
        var productId = await CreateProductAsync(archived: archivedProduct);
        await AddOpeningStockAsync(warehouseId, productId, quantity, unitCost: 10m);

        if (sellOut)
        {
            await AdjustAsync(warehouseId, productId, "Decrease", quantity, "Damage");
        }

        await SetLowStockThresholdAsync(warehouseId, productId, threshold);

        return productId;
    }
}
