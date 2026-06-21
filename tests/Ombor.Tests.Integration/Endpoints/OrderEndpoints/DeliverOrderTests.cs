using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.OrderEndpoints;

public sealed class DeliverOrderTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : OrderTestsBase(factory, output)
{
    [Fact]
    public async Task Deliver_ShouldPromoteToSale_DecrementStock_AndCreateReceivable()
    {
        // Arrange — a shipped order for 2 units @ 100 (total 200), stock available.
        var customerId = await CreateCustomerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100, averageCost: 50m);
        var shipped = await CreateOrderInStatusAsync(customerId, productId, OrderStatus.Shipping);

        // Act
        var delivered = await _client.PostAsync<OrderDto>(
            $"{Routes.Order}/{shipped.Id}/deliver", new { warehouseId }, HttpStatusCode.OK);

        // Assert — order promoted.
        Assert.Equal("Delivered", delivered.Status);
        Assert.NotNull(delivered.SaleId);

        // Stock decremented by the ordered quantity.
        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(98, item.Quantity);

        // A Sale on account exists for the order total, fully unpaid.
        var sale = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == delivered.SaleId!.Value);
        Assert.Equal(TransactionType.Sale, sale.Type);
        Assert.Equal(warehouseId, sale.WarehouseId);
        Assert.Equal(200m, sale.TotalDue);
        Assert.Equal(0m, sale.TotalPaid);

        // Receivable increased by the sale.
        var balance = await _context.PartnerBalances.FirstAsync(b => b.PartnerId == customerId);
        Assert.Equal(200m, balance.Total);
    }

    [Fact]
    public async Task Deliver_SaleLinesShouldMirrorOrderLines()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 50);
        var shipped = await CreateOrderInStatusAsync(customerId, productId, OrderStatus.Shipping);

        // Act
        var delivered = await _client.PostAsync<OrderDto>(
            $"{Routes.Order}/{shipped.Id}/deliver", new { warehouseId }, HttpStatusCode.OK);

        // Assert
        var sale = await _context.Transactions.AsNoTracking()
            .Include(t => t.Lines)
            .FirstAsync(t => t.Id == delivered.SaleId!.Value);
        var saleLine = Assert.Single(sale.Lines);
        Assert.Equal(productId, saleLine.ProductId);
        Assert.Equal(2, saleLine.Quantity);
        Assert.Equal(100m, saleLine.UnitPrice);
    }

    [Fact]
    public async Task Deliver_ShouldReturnBadRequest_AndRollBack_WhenInsufficientStock()
    {
        // Arrange — only 1 unit in stock, but the order needs 2.
        var customerId = await CreateCustomerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 1);
        var shipped = await CreateOrderInStatusAsync(customerId, productId, OrderStatus.Shipping);

        // Act
        await _client.PostAsync<ProblemDetails>(
            $"{Routes.Order}/{shipped.Id}/deliver", new { warehouseId }, HttpStatusCode.BadRequest);

        // Assert — nothing was applied: no sale, stock unchanged, order still Shipping, no saleId.
        var order = await _context.Orders.AsNoTracking().FirstAsync(o => o.Id == shipped.Id);
        Assert.Equal(OrderStatus.Shipping, order.Status);
        Assert.Null(order.SaleId);

        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(1, item.Quantity);

        Assert.False(await _context.Transactions.AsNoTracking().AnyAsync(t => t.PartnerId == customerId));
    }

    [Fact]
    public async Task Deliver_ShouldReturnBadRequest_WhenWarehouseDoesNotExist()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var shipped = await CreateOrderInStatusAsync(customerId, productId, OrderStatus.Shipping);

        // Act + Assert
        await _client.PostAsync<ProblemDetails>(
            $"{Routes.Order}/{shipped.Id}/deliver", new { warehouseId = NonExistentEntityId }, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deliver_FromProcessing_ShouldReturnConflict_AndPromoteNothing()
    {
        // Arrange — only a Shipping order can be delivered.
        var customerId = await CreateCustomerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);
        var processing = await CreateOrderInStatusAsync(customerId, productId, OrderStatus.Processing);

        // Act
        await _client.PostAsync<ProblemDetails>(
            $"{Routes.Order}/{processing.Id}/deliver", new { warehouseId }, HttpStatusCode.Conflict);

        // Assert — nothing promoted.
        var order = await _context.Orders.AsNoTracking().FirstAsync(o => o.Id == processing.Id);
        Assert.Null(order.SaleId);

        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(100, item.Quantity);
    }

    [Fact]
    public async Task NoStockReservation_UntilDelivered()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        // Act — walk to Shipping without delivering.
        await CreateOrderInStatusAsync(customerId, productId, OrderStatus.Shipping);

        // Assert — stock is untouched until delivery.
        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(100, item.Quantity);
    }
}
