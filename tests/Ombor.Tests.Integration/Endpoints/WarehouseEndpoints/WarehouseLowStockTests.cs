using System.Net;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

/// <summary>
/// Low stock by warehouse-item threshold (DR-41): the «Остатки» rows serve their own threshold and flag, and the warehouse
/// serves «Заканчивается» as a count of exactly the flagged rows. Each test stocks a fresh warehouse with fresh products.
/// </summary>
public sealed class WarehouseLowStockTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WarehouseTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task Stock_ServesEachRowsThreshold_AndFlagsOnlyTrackedActiveRowsAtOrBelowIt()
    {
        var warehouse = await CreateWarehouseAsync();
        var plenty = await StockAsync(warehouse.Id, quantity: 20m, threshold: 10m);
        var scarce = await StockAsync(warehouse.Id, quantity: 5m, threshold: 10m);
        var untrackedEmpty = await StockAsync(warehouse.Id, quantity: 4m, threshold: null, sellOut: true);
        var trackedEmpty = await StockAsync(warehouse.Id, quantity: 4m, threshold: 3m, sellOut: true);
        var archived = await StockAsync(warehouse.Id, quantity: 2m, threshold: 10m, archivedProduct: true);

        var stock = await _client.GetAsync<WarehouseStockItemDto[]>($"{GetUrl(warehouse.Id)}/stock");

        Assert.Equal(5, stock.Length);
        AssertRow(stock, plenty, threshold: 10m, isLow: false);
        AssertRow(stock, scarce, threshold: 10m, isLow: true);
        AssertRow(stock, untrackedEmpty, threshold: null, isLow: false);
        AssertRow(stock, trackedEmpty, threshold: 3m, isLow: true);
        AssertRow(stock, archived, threshold: 10m, isLow: false);
        Assert.Equal(0m, Assert.Single(stock, r => r.ProductId == untrackedEmpty).Quantity);

        // «Товаров» counts products on hand (the two emptied rows are not); «Заканчивается» counts the flagged rows.
        var fetched = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id));
        Assert.Equal(3, fetched.ProductCount);
        Assert.Equal(2, fetched.LowStockCount);

        var listed = Assert.Single(await _client.GetAsync<WarehouseDto[]>(GetUrl()), w => w.Id == warehouse.Id);
        Assert.Equal(3, listed.ProductCount);
        Assert.Equal(2, listed.LowStockCount);
    }

    [Fact]
    public async Task ArchivedWarehouse_CountsNothingLow_ButKeepsItsThresholds()
    {
        var warehouse = await CreateWarehouseAsync();
        var scarce = await StockAsync(warehouse.Id, quantity: 2m, threshold: 10m);

        var archived = await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/archive", content: new { }, HttpStatusCode.OK);
        var archivedStock = await _client.GetAsync<WarehouseStockItemDto[]>($"{GetUrl(warehouse.Id)}/stock");

        Assert.Equal(0, archived.LowStockCount);
        AssertRow(archivedStock, scarce, threshold: 10m, isLow: false);

        var restored = await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/restore", content: new { }, HttpStatusCode.OK);

        Assert.Equal(1, restored.LowStockCount);
    }

    private static void AssertRow(WarehouseStockItemDto[] stock, int productId, decimal? threshold, bool isLow)
    {
        var row = Assert.Single(stock, r => r.ProductId == productId);
        Assert.Equal(threshold, row.LowStockThreshold);
        Assert.Equal(isLow, row.IsLowStock);
    }

    /// <summary>A fresh product stocked through opening stock, then given its threshold; written off to zero when asked.</summary>
    private async Task<int> StockAsync(
        int warehouseId, decimal quantity, decimal? threshold, bool sellOut = false, bool archivedProduct = false)
    {
        var product = await CreateProductAsync(archivedProduct);
        await _client.PostAsync<WarehouseDto>(
            $"{GetUrl(warehouseId)}/opening-stock",
            new AddOpeningStockRequest(warehouseId, [new OpeningStockLine(product.Id, quantity, UnitCost: 10m)]),
            HttpStatusCode.OK);

        if (sellOut)
        {
            await _client.PostAsync<StockAdjustmentDto>(
                Routes.StockAdjustment,
                new { warehouseId, productId = product.Id, direction = "Decrease", quantity, reason = "Damage", note = (string?)null });
        }

        await SetLowStockThresholdAsync(warehouseId, product.Id, threshold);

        return product.Id;
    }
}
