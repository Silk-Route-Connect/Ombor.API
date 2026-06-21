using System.Net;
using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.OrderEndpoints;

public sealed class UpdateOrderTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : OrderTestsBase(factory, output)
{
    [Fact]
    public async Task Put_ShouldReplaceLinesAndRecomputeTotal()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId, quantity: 2, unitPrice: 100m)); // total 200

        var body = new
        {
            id = created.Id,
            customerId,
            source = "OmborWeb",
            lines = new[] { new { productId, quantity = 3, unitPrice = 100m, discount = (decimal?)null, discountType = "Fixed" } },
        };

        // Act
        var updated = await _client.PutAsync<OrderDto>($"{Routes.Order}/{created.Id}", body);

        // Assert
        Assert.Equal(300m, updated.Total);
        Assert.Equal(3, Assert.Single(updated.Lines).Quantity);
    }

    [Fact]
    public async Task Put_ShouldSetWarehouse_WhenValueProvided()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId)); // no warehouse

        var body = new { id = created.Id, customerId, source = "OmborWeb", warehouseId, lines = OneLine(productId) };

        // Act
        var updated = await _client.PutAsync<OrderDto>($"{Routes.Order}/{created.Id}", body);

        // Assert
        Assert.Equal(warehouseId, updated.WarehouseId);
    }

    [Fact]
    public async Task Put_ShouldKeepWarehouse_WhenFieldOmitted()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId, warehouseId: warehouseId));

        // warehouseId omitted → keep existing.
        var body = new { id = created.Id, customerId, source = "OmborWeb", lines = OneLine(productId) };

        // Act
        var updated = await _client.PutAsync<OrderDto>($"{Routes.Order}/{created.Id}", body);

        // Assert
        Assert.Equal(warehouseId, updated.WarehouseId);
    }

    [Fact]
    public async Task Put_ShouldClearWarehouse_WhenFieldNull()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId, warehouseId: warehouseId));

        // warehouseId explicitly null → clear.
        var body = new { id = created.Id, customerId, source = "OmborWeb", warehouseId = (int?)null, lines = OneLine(productId) };

        // Act
        var updated = await _client.PutAsync<OrderDto>($"{Routes.Order}/{created.Id}", body);

        // Assert
        Assert.Null(updated.WarehouseId);
    }

    [Fact]
    public async Task Put_ShouldReturnBadRequest_WhenOrderNotOpen()
    {
        // Arrange — a shipped order can no longer be edited.
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var shipped = await CreateOrderInStatusAsync(customerId, productId, OrderStatus.Shipping);

        var body = new { id = shipped.Id, customerId, source = "OmborWeb", lines = OneLine(productId) };

        // Act + Assert
        await _client.PutAsync($"{Routes.Order}/{shipped.Id}", body, HttpStatusCode.BadRequest);
    }

    private static object OneLine(int productId)
        => new[] { new { productId, quantity = 1, unitPrice = 50m, discount = (decimal?)null, discountType = "Fixed" } };
}
