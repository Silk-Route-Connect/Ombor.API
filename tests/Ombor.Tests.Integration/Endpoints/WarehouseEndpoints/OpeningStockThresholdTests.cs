using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

/// <summary>
/// POST /api/warehouses/{id}/opening-stock with a «Порог» per line (DR-41): a line's threshold becomes the threshold of
/// the row it creates, in the same write.
/// </summary>
public sealed class OpeningStockThresholdTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WarehouseLowStockTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task OpeningStock_SetsTheThresholdOfEachLineThatNamesOne_OthersStayUntracked()
    {
        var warehouse = await CreateWarehouseAsync();
        var tracked = await CreateProductAsync();
        var untracked = await CreateProductAsync();

        var response = await PostOpeningStockAsync(
            warehouse.Id,
            new OpeningStockLine(tracked.Id, Quantity: 5m, UnitCost: 10m, LowStockThreshold: 10m),
            new OpeningStockLine(untracked.Id, Quantity: 3m, UnitCost: 10m));

        Assert.Equal(2, response.ProductCount);
        Assert.Equal(1, response.LowStockCount);

        var stock = await GetStockAsync(warehouse.Id);
        AssertRow(stock, tracked.Id, threshold: 10m, isLow: true);
        AssertRow(stock, untracked.Id, threshold: null, isLow: false);
    }

    [Fact]
    public async Task OpeningStock_RepeatedProduct_TakesTheLastLineThatNamesAThreshold()
    {
        var warehouse = await CreateWarehouseAsync();
        var namedThenBlank = await CreateProductAsync();
        var namedTwice = await CreateProductAsync();

        // Repeated lines are summed into one row: 5 + 3 = 8 under 50, and 5 + 1 = 6 under 7.
        var response = await PostOpeningStockAsync(
            warehouse.Id,
            new OpeningStockLine(namedThenBlank.Id, Quantity: 5m, UnitCost: 10m, LowStockThreshold: 50m),
            new OpeningStockLine(namedTwice.Id, Quantity: 5m, UnitCost: 10m, LowStockThreshold: 2m),
            new OpeningStockLine(namedThenBlank.Id, Quantity: 3m, UnitCost: 10m),
            new OpeningStockLine(namedTwice.Id, Quantity: 1m, UnitCost: 10m, LowStockThreshold: 7m));

        Assert.Equal(2, response.ProductCount);
        Assert.Equal(2, response.LowStockCount);

        var stock = await GetStockAsync(warehouse.Id);
        AssertRow(stock, namedThenBlank.Id, threshold: 50m, isLow: true);
        AssertRow(stock, namedTwice.Id, threshold: 7m, isLow: true);
        Assert.Equal(8m, Assert.Single(stock, r => r.ProductId == namedThenBlank.Id).Quantity);
    }

    [Fact]
    public async Task OpeningStock_RejectsANegativeThreshold_AndNeverTouchesAnExistingRow()
    {
        var warehouse = await CreateWarehouseAsync();
        var fresh = await CreateProductAsync();
        var stocked = await StockAsync(warehouse.Id, quantity: 5m, threshold: 7m);

        var negative = await _client.PostAsync<ValidationProblemDetails>(
            $"{GetUrl(warehouse.Id)}/opening-stock",
            new AddOpeningStockRequest(warehouse.Id, [new OpeningStockLine(fresh.Id, Quantity: 5m, UnitCost: 10m, LowStockThreshold: -1m)]),
            HttpStatusCode.BadRequest);
        Assert.Contains("Items[0].LowStockThreshold", negative.Errors.Keys);

        // A product already stocked is rejected whatever its line says, so an opening line can never change or clear an
        // existing row's threshold.
        await _client.PostAsync<ValidationProblemDetails>(
            $"{GetUrl(warehouse.Id)}/opening-stock",
            new AddOpeningStockRequest(warehouse.Id, [new OpeningStockLine(stocked, Quantity: 5m, UnitCost: 10m)]),
            HttpStatusCode.BadRequest);
        Assert.Equal(7m, await ThresholdInDbAsync(warehouse.Id, stocked));
        Assert.False(await _context.WarehouseItems.AnyAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == fresh.Id));
    }

    private Task<WarehouseDto> PostOpeningStockAsync(int warehouseId, params OpeningStockLine[] lines) =>
        _client.PostAsync<WarehouseDto>(
            $"{GetUrl(warehouseId)}/opening-stock", new AddOpeningStockRequest(warehouseId, lines), HttpStatusCode.OK);
}
