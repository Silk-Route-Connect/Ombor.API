using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.OrderEndpoints;

public sealed class GetOrdersTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : OrderTestsBase(factory, output)
{
    [Fact]
    public async Task Get_ShouldReturnNewestFirst()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var first = await PostOrderAsync(BuildCreateBody(customerId, productId));
        var second = await PostOrderAsync(BuildCreateBody(customerId, productId));
        var third = await PostOrderAsync(BuildCreateBody(customerId, productId));

        // Act — scope to this customer so the shared DB's other orders don't interfere.
        var list = await _client.GetAsync<OrderDto[]>($"{Routes.Order}?customerId={customerId}");

        // Assert
        Assert.Equal(3, list.Length);
        Assert.Equal(third.Id, list[0].Id);
        Assert.Equal(second.Id, list[1].Id);
        Assert.Equal(first.Id, list[2].Id);
    }

    [Fact]
    public async Task Get_ShouldFilterByStatus()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        await PostOrderAsync(BuildCreateBody(customerId, productId)); // stays Pending
        var processed = await CreateOrderInStatusAsync(customerId, productId, OrderStatus.Processing);

        // Act
        var list = await _client.GetAsync<OrderDto[]>($"{Routes.Order}?customerId={customerId}&status=Processing");

        // Assert
        Assert.Single(list);
        Assert.Equal(processed.Id, list[0].Id);
        Assert.Equal("Processing", list[0].Status);
    }
}
