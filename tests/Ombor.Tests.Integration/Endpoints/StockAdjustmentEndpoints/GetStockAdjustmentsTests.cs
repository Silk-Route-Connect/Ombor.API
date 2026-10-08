using System.Net;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.StockAdjustmentEndpoints;

public sealed class GetStockAdjustmentsTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : StockAdjustmentTestsBase(factory, output)
{
    [Fact]
    public async Task Get_ShouldReturnNewestFirst_FilteredByWarehouse()
    {
        // Arrange
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);
        var first = await PostAdjustmentAsync(warehouseId, productId, "Increase", 2, "Found");
        var second = await PostAdjustmentAsync(warehouseId, productId, "Decrease", 1, "Damage");

        // Act — scope to this warehouse (the shared DB holds other adjustments).
        var list = await _client.GetAsync<StockAdjustmentDto[]>($"{Routes.StockAdjustment}?warehouseId={warehouseId}");

        // Assert
        Assert.Equal(2, list.Length);
        Assert.Equal(second.Id, list[0].Id); // newest first
        Assert.Equal(first.Id, list[1].Id);
        Assert.All(list, a => Assert.Equal(warehouseId, a.WarehouseId));
    }

    [Fact]
    public async Task Get_ShouldReportHistoricalBalanceAfter_PerAdjustment()
    {
        // Arrange — opening stock as an event so the derived ledger reconciles.
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 100, unitCost: 10m); // 100

        var decrease = await PostAdjustmentAsync(warehouseId, productId, "Decrease", 30, "Damage"); //  70
        var increase = await PostAdjustmentAsync(warehouseId, productId, "Increase", 50, "Found");   // 120

        // Act
        var list = await _client.GetAsync<StockAdjustmentDto[]>(
            $"{Routes.StockAdjustment}?warehouseId={warehouseId}&productId={productId}");

        // Assert — each row shows the balance AT its point in time (newest-first), not the current stock.
        Assert.Equal(increase.Id, list[0].Id);
        Assert.Equal(120, list[0].BalanceAfter);
        Assert.Equal(decrease.Id, list[1].Id);
        Assert.Equal(70, list[1].BalanceAfter); // historical: 70, though current stock is 120

        // The listed figure matches what create returned for the same event.
        Assert.Equal(decrease.BalanceAfter, list[1].BalanceAfter);
    }

    [Fact]
    public async Task GetById_ShouldServeTheBalanceAfter_OfTheWarehouseLedger()
    {
        // Arrange — later events (another adjustment, a transfer out) must not move this row's historical figure.
        var warehouseId = await CreateWarehouseAsync();
        var otherWarehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 100, unitCost: 10m);
        var decrease = await PostAdjustmentAsync(warehouseId, productId, "Decrease", 30, "Damage"); // 70
        await _client.PostAsync<object>(
            Routes.Transfer,
            new { fromWarehouseId = warehouseId, toWarehouseId = otherWarehouseId, lines = new[] { new { productId, quantity = 20 } } }); // 50
        var increase = await PostAdjustmentAsync(warehouseId, productId, "Increase", 5, "Found"); // 55

        // Act
        var first = await _client.GetAsync<StockAdjustmentDto>($"{Routes.StockAdjustment}/{decrease.Id}");
        var second = await _client.GetAsync<StockAdjustmentDto>($"{Routes.StockAdjustment}/{increase.Id}");
        var list = await _client.GetAsync<StockAdjustmentDto[]>($"{Routes.StockAdjustment}?warehouseId={warehouseId}");
        var ledger = await _client.GetAsync<WarehouseMovementDto[]>($"{Routes.Warehouse}/{warehouseId}/movements");

        // Assert — one figure per event, whichever endpoint serves it.
        Assert.Equal(decrease.Id, first.Id);
        Assert.Equal(70, first.BalanceAfter);
        Assert.Equal(55, second.BalanceAfter);
        Assert.Equal(30, first.Quantity);
        foreach (var adjustment in list)
        {
            var row = Assert.Single(ledger, m => m.Kind == MovementKind.Adjustment && m.Id == adjustment.Id);
            Assert.Equal(row.BalanceAfter, adjustment.BalanceAfter);
        }
    }

    [Fact]
    public async Task GetById_ShouldBeNotFound_ForAnUnknownId()
    {
        await _client.GetAsync($"{Routes.StockAdjustment}/{NonExistentEntityId}", HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldFilterByProduct()
    {
        // Arrange
        var warehouseId = await CreateWarehouseAsync();
        var productA = await CreateProductAsync();
        var productB = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productA, quantity: 100);
        await SeedStockAsync(warehouseId, productB, quantity: 100);
        await PostAdjustmentAsync(warehouseId, productA, "Increase", 1, "Found");
        await PostAdjustmentAsync(warehouseId, productB, "Increase", 1, "Found");

        // Act
        var list = await _client.GetAsync<StockAdjustmentDto[]>(
            $"{Routes.StockAdjustment}?warehouseId={warehouseId}&productId={productA}");

        // Assert
        Assert.Single(list);
        Assert.Equal(productA, list[0].ProductId);
    }
}
