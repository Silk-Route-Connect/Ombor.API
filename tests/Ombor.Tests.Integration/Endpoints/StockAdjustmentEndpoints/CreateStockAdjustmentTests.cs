using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.StockAdjustmentEndpoints;

public sealed class CreateStockAdjustmentTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : StockAdjustmentTestsBase(factory, output)
{
    [Fact]
    public async Task Increase_ShouldAddStock_AtCarryingCost()
    {
        // Arrange — 10 units @ WAC 50.
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10, averageCost: 50m);

        // Act
        var dto = await PostAdjustmentAsync(warehouseId, productId, "Increase", quantity: 5, reason: "Found");

        // Assert
        Assert.Equal("Increase", dto.Direction);
        Assert.Equal(5, dto.Quantity);

        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(15, item.Quantity);      // +5
        Assert.Equal(50m, item.AverageCost);  // WAC unchanged on a carrying-cost stock-in
    }

    [Fact]
    public async Task Create_ShouldReturnBalanceAfter_AsLiveStock()
    {
        // Arrange — 100 on hand.
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        // Act + Assert — balanceAfter is the stock right after each adjustment.
        var decrease = await PostAdjustmentAsync(warehouseId, productId, "Decrease", quantity: 30, reason: "Damage");
        Assert.Equal(70, decrease.BalanceAfter);

        var increase = await PostAdjustmentAsync(warehouseId, productId, "Increase", quantity: 10, reason: "Found");
        Assert.Equal(80, increase.BalanceAfter);
    }

    [Fact]
    public async Task Decrease_ShouldRemoveStock_AtWac_AndSnapshotLossCost()
    {
        // Arrange — 10 units @ WAC 50.
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10, averageCost: 50m);

        // Act
        var dto = await PostAdjustmentAsync(warehouseId, productId, "Decrease", quantity: 4, reason: "Damage");

        // Assert
        Assert.Equal("Decrease", dto.Direction);

        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(6, item.Quantity);       // −4
        Assert.Equal(50m, item.AverageCost);  // WAC unchanged on stock-out

        // The loss is recorded at the carrying cost (WAC snapshot).
        var adjustment = await _context.StockAdjustments.AsNoTracking().FirstAsync(a => a.Id == dto.Id);
        Assert.Equal(50m, adjustment.UnitCost);
    }

    [Fact]
    public async Task Decrease_ShouldReturnBadRequest_AndRollBack_WhenInsufficientStock()
    {
        // Arrange — only 3 in stock.
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 3);

        // Act
        await _client.PostAsync<ProblemDetails>(
            Routes.StockAdjustment,
            new { warehouseId, productId, direction = "Decrease", quantity = 5, reason = "Theft" },
            HttpStatusCode.BadRequest);

        // Assert — nothing applied: no adjustment recorded, stock unchanged.
        Assert.False(await _context.StockAdjustments.AsNoTracking().AnyAsync(a => a.WarehouseId == warehouseId));
        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenReasonInvalidForDirection()
    {
        // Arrange — "Damage" is a decrease-only reason.
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10);

        // Act + Assert
        var problem = await _client.PostAsync<ValidationProblemDetails>(
            Routes.StockAdjustment,
            new { warehouseId, productId, direction = "Increase", quantity = 1, reason = "Damage" },
            HttpStatusCode.BadRequest);
        Assert.NotNull(problem);
    }
}
