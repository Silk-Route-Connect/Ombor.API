using Ombor.Contracts.Responses.Transfer;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TransferEndpoints;

public sealed class GetTransfersTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransferTestsBase(factory, output)
{
    [Fact]
    public async Task Get_ShouldReturnNewestFirst_FilteredByWarehouse()
    {
        // Arrange
        var fromId = await CreateWarehouseAsync();
        var toId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(fromId, productId, quantity: 100);
        var first = await PostTransferAsync(fromId, toId, productId, quantity: 1);
        var second = await PostTransferAsync(fromId, toId, productId, quantity: 1);

        // Act — the warehouse filter matches transfers touching either side.
        var list = await _client.GetAsync<TransferDto[]>($"{Routes.Transfer}?warehouseId={toId}");

        // Assert
        Assert.Equal(2, list.Length);
        Assert.Equal(second.Id, list[0].Id); // newest first
        Assert.Equal(first.Id, list[1].Id);
    }
}
