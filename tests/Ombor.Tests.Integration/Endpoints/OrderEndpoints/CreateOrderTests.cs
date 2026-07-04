using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.Order;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.OrderEndpoints;

public sealed class CreateOrderTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : OrderTestsBase(factory, output)
{
    [Fact]
    public async Task PostAsync_ShouldCreatePendingOrderWithCreationHistory()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();

        // Act
        var order = await PostOrderAsync(BuildCreateBody(customerId, productId, quantity: 2, unitPrice: 100m));

        // Assert
        Assert.Equal("Pending", order.Status);
        Assert.Null(order.SaleId);
        Assert.Equal(200m, order.Total);

        var line = Assert.Single(order.Lines);
        Assert.Equal(200m, line.Total);
        Assert.Equal("Fixed", line.DiscountType);
        Assert.False(string.IsNullOrEmpty(line.Sku));
        Assert.False(string.IsNullOrEmpty(line.Measurement));

        var creation = Assert.Single(order.History);
        Assert.Null(creation.From);
        Assert.Equal("Pending", creation.To);
    }

    [Fact]
    public async Task PostAsync_ShouldNotReserveStock()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        // Act
        await PostOrderAsync(BuildCreateBody(customerId, productId, warehouseId: warehouseId));

        // Assert — an order reserves nothing; stock is untouched.
        var item = await _context.WarehouseItems
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(100, item.Quantity);
    }

    [Theory]
    [InlineData("Percentage", 10, 180)]   // 200 gross − 10% = 180
    [InlineData("Percentage", 150, 0)]    // clamped to 100%
    [InlineData("Fixed", 50, 150)]        // 200 − 50
    [InlineData("Fixed", 500, 0)]         // clamped to the line gross
    public async Task PostAsync_ShouldApplyRule37LineDiscount(string discountType, decimal discount, decimal expectedTotal)
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();

        // Act
        var order = await PostOrderAsync(
            BuildCreateBody(customerId, productId, quantity: 2, unitPrice: 100m, discount: discount, discountType: discountType));

        // Assert
        Assert.Equal(expectedTotal, order.Total);
        Assert.Equal(discountType, Assert.Single(order.Lines).DiscountType);
    }

    [Fact]
    public async Task PostAsync_ShouldReflectCustomerBalance()
    {
        // Arrange — an open receivable Sale makes the customer owe us.
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        await CreateOpenSaleAsync(customerId, due: 7_500m);

        // Act
        var order = await PostOrderAsync(BuildCreateBody(customerId, productId));

        // Assert
        Assert.Equal(7_500m, order.CustomerBalance);
    }

    [Fact]
    public async Task PostAsync_ShouldRoundTripNullDeliveryAddress()
    {
        // Arrange — no delivery address: the Address complex type is materialized all-null on read-back.
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var body = new
        {
            customerId,
            source = "OmborWeb",
            deliveryAddress = (string?)null,
            lines = new[] { new { productId, quantity = 1, unitPrice = 100m, discount = (decimal?)null, discountType = "Fixed" } },
        };

        // Act
        var created = await PostOrderAsync(body);
        var fetched = await _client.GetAsync<OrderDto>($"{Routes.Order}/{created.Id}");

        // Assert — both the create projection and a fresh GET handle the all-null address.
        Assert.Null(created.DeliveryAddress);
        Assert.Null(fetched.DeliveryAddress);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenNoLines()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var body = new { customerId, source = "OmborWeb", lines = Array.Empty<object>() };

        // Act + Assert
        var problem = await _client.PostAsync<ValidationProblemDetails>(Routes.Order, body, HttpStatusCode.BadRequest);
        Assert.NotNull(problem);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenSourceEnumInvalid()
    {
        // Arrange — a body valid except for an unparseable OrderSource. The invalid enum must surface as a
        // 400 ValidationProblemDetails, not a 500 (delta §10 / F-017): the binding failure is swallowed into
        // model state, so without the model-state filter the action would run with a null request and 500.
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var body = new
        {
            customerId,
            source = "NotARealSource",
            deliveryAddress = "Test address",
            lines = new[] { new { productId, quantity = 1, unitPrice = 100m, discount = (decimal?)null, discountType = "Fixed" } },
        };

        // Act + Assert
        var problem = await _client.PostAsync<ValidationProblemDetails>(Routes.Order, body, HttpStatusCode.BadRequest);
        Assert.NotNull(problem);
    }
}
