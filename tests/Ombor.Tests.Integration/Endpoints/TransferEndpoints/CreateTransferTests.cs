using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TransferEndpoints;

public sealed class CreateTransferTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransferTestsBase(factory, output)
{
    [Fact]
    public async Task Create_ShouldMoveStockAtomically_CarryingSourceWac()
    {
        // Arrange — source 10 @ WAC 50, destination already has 4 @ WAC 100.
        var fromId = await CreateWarehouseAsync();
        var toId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(fromId, productId, quantity: 10, averageCost: 50m);
        await SeedStockAsync(toId, productId, quantity: 4, averageCost: 100m);

        // Act — move 6 units.
        var dto = await PostTransferAsync(fromId, toId, productId, quantity: 6);

        // Assert — DTO shape.
        Assert.Equal(fromId, dto.FromWarehouseId);
        Assert.Equal(toId, dto.ToWarehouseId);
        var line = Assert.Single(dto.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(6, line.Quantity);
        Assert.False(string.IsNullOrEmpty(line.Sku));
        Assert.False(string.IsNullOrEmpty(line.Measurement));

        // Source decremented, WAC unchanged (stock-out leaves at WAC).
        var source = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == fromId && i.ProductId == productId);
        Assert.Equal(4, source.Quantity);
        Assert.Equal(50m, source.AverageCost);

        // Destination incremented; WAC weighted in at the source's cost: (4×100 + 6×50) / 10 = 70.
        var dest = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == toId && i.ProductId == productId);
        Assert.Equal(10, dest.Quantity);
        Assert.Equal(70m, dest.AverageCost);
    }

    [Fact]
    public async Task Create_ShouldCreateDestinationItem_WhenDestinationHadNone()
    {
        // Arrange — destination has no stock of this product.
        var fromId = await CreateWarehouseAsync();
        var toId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(fromId, productId, quantity: 10, averageCost: 50m);

        // Act
        await PostTransferAsync(fromId, toId, productId, quantity: 4);

        // Assert — destination created at the source's carrying cost.
        var dest = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == toId && i.ProductId == productId);
        Assert.Equal(4, dest.Quantity);
        Assert.Equal(50m, dest.AverageCost);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_AndRollBack_WhenInsufficientSourceStock()
    {
        // Arrange — only 3 in the source, moving 5.
        var fromId = await CreateWarehouseAsync();
        var toId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(fromId, productId, quantity: 3);

        // Act
        await _client.PostAsync<ProblemDetails>(
            Routes.Transfer,
            new { fromWarehouseId = fromId, toWarehouseId = toId, note = (string?)null, lines = new[] { new { productId, quantity = 5 } } },
            HttpStatusCode.BadRequest);

        // Assert — nothing applied: no transfer, source unchanged, destination item not created.
        Assert.False(await _context.Transfers.AsNoTracking().AnyAsync(t => t.FromWarehouseId == fromId));

        var source = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == fromId && i.ProductId == productId);
        Assert.Equal(3, source.Quantity);

        Assert.False(await _context.WarehouseItems.AsNoTracking()
            .AnyAsync(i => i.WarehouseId == toId && i.ProductId == productId));
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenSameWarehouse()
    {
        // Arrange
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10);

        // Act + Assert
        await _client.PostAsync<ValidationProblemDetails>(
            Routes.Transfer,
            new { fromWarehouseId = warehouseId, toWarehouseId = warehouseId, note = (string?)null, lines = new[] { new { productId, quantity = 1 } } },
            HttpStatusCode.BadRequest);
    }
}
