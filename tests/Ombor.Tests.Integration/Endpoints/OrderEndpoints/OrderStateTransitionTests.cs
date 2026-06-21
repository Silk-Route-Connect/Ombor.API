using System.Net;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.OrderEndpoints;

public sealed class OrderStateTransitionTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : OrderTestsBase(factory, output)
{
    [Fact]
    public async Task Process_ShouldFlipStatusAndAppendHistory()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId));

        // Act
        var processed = await PostStateAsync(created.Id, "process");

        // Assert
        Assert.Equal("Processing", processed.Status);
        Assert.Equal(2, processed.History.Length); // creation + process
        var last = processed.History[^1];
        Assert.Equal("Pending", last.From);
        Assert.Equal("Processing", last.To);
    }

    [Fact]
    public async Task Cancel_FromPending_ShouldSucceed()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId));

        // Act
        var cancelled = await PostStateAsync(created.Id, "cancel");

        // Assert
        Assert.Equal("Cancelled", cancelled.Status);
    }

    [Fact]
    public async Task Ship_FromPending_ShouldReturnConflict()
    {
        // Arrange — Pending → Shipping skips Processing and is illegal.
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId));

        // Act + Assert
        await _client.PostAsync($"{Routes.Order}/{created.Id}/ship", HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deliver_FromPending_ShouldReturnConflict()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var productId = await CreateProductAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId));

        // Act + Assert
        await _client.PostAsync($"{Routes.Order}/{created.Id}/deliver", HttpStatusCode.Conflict);
    }
}
