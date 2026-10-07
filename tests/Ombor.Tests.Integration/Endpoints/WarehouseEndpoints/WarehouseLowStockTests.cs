using System.Net;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

/// <summary>
/// Low stock by warehouse-item threshold (DR-41): the «Остатки» rows serve their own threshold and flag, and the
/// warehouse serves «Заканчивается» as a count of exactly the flagged rows.
/// </summary>
public sealed class WarehouseLowStockTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WarehouseLowStockTestsBase(factory, outputHelper)
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

        var stock = await GetStockAsync(warehouse.Id);

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
    public async Task ZeroThreshold_IsTracked_AndLowOnlyOnceTheRowIsEmpty()
    {
        var warehouse = await CreateWarehouseAsync();
        var productId = await StockAsync(warehouse.Id, quantity: 5m, threshold: null);

        // Zero is a threshold, not «not tracked»: it is served as 0 and the row is not low while it holds stock.
        var tracked = await PutThresholdAsync(warehouse.Id, productId, 0m);

        Assert.Equal(0m, tracked.LowStockThreshold);
        Assert.False(tracked.IsLowStock);
        Assert.Equal(0, (await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id))).LowStockCount);

        await WriteOffAsync(warehouse.Id, productId, 5m);

        AssertRow(await GetStockAsync(warehouse.Id), productId, threshold: 0m, isLow: true);
        var emptied = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id));
        Assert.Equal(0, emptied.ProductCount);
        Assert.Equal(1, emptied.LowStockCount);
    }

    [Fact]
    public async Task ArchivedWarehouse_CountsNothingLow_ButKeepsItsThresholds()
    {
        var warehouse = await CreateWarehouseAsync();
        var scarce = await StockAsync(warehouse.Id, quantity: 2m, threshold: 10m);

        var archived = await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/archive", content: new { }, HttpStatusCode.OK);
        var archivedStock = await GetStockAsync(warehouse.Id);

        Assert.Equal(0, archived.LowStockCount);
        AssertRow(archivedStock, scarce, threshold: 10m, isLow: false);

        var restored = await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/restore", content: new { }, HttpStatusCode.OK);

        Assert.Equal(1, restored.LowStockCount);
    }
}
