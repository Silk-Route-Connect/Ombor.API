using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Ombor.Domain.Exceptions;
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

        // Act
        var problem = await _client.PostAsync<JObject>($"{Routes.Order}/{created.Id}/ship", new { }, HttpStatusCode.Conflict);

        // Assert — the 409 carries its code and the two statuses, so the client can say what was refused.
        Assert.Equal(ErrorCodes.OrderInvalidTransition, (string?)problem["code"]);
        Assert.Equal("Pending", (string?)problem["params"]?["from"]);
        Assert.Equal("Shipping", (string?)problem["params"]?["to"]);
    }

    [Fact]
    public async Task Deliver_FromPending_ShouldReturnConflict()
    {
        // Arrange — the illegal transition is rejected before any stock/warehouse work.
        var customerId = await CreateCustomerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        var created = await PostOrderAsync(BuildCreateBody(customerId, productId));

        // Act + Assert
        await _client.PostAsync<ProblemDetails>(
            $"{Routes.Order}/{created.Id}/deliver", new { warehouseId }, HttpStatusCode.Conflict);
    }
}
