using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

public class WarehouseStockTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : WarehouseTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task OpeningStock_ShouldComputeWarehouseTotals_AndExposePerProductStock()
    {
        // Arrange — an empty warehouse and two products.
        var warehouse = await CreateWarehouseAsync();
        var productA = await CreateProductAsync();
        var productB = await CreateProductAsync();

        var request = new AddOpeningStockRequest(
            warehouse.Id,
            [
                new OpeningStockLine(productA.Id, Quantity: 10, UnitCost: 100m),
                new OpeningStockLine(productB.Id, Quantity: 5, UnitCost: 200m),
            ]);
        var openingStockUrl = $"{GetUrl(warehouse.Id)}/opening-stock";

        // Act
        var afterOpeningStock = await _client.PostAsync<WarehouseDto>(openingStockUrl, request, System.Net.HttpStatusCode.OK);

        // Assert — totals are server-computed (rule 12): 2 products, 15 units, 10*100 + 5*200 = 2000.
        Assert.Equal(2, afterOpeningStock.ProductCount);
        Assert.Equal(15, afterOpeningStock.TotalUnits);
        Assert.Equal(2_000m, afterOpeningStock.StockValue);

        // GET {id} recomputes the same totals.
        var fetched = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id));
        Assert.Equal(2, fetched.ProductCount);
        Assert.Equal(15, fetched.TotalUnits);
        Assert.Equal(2_000m, fetched.StockValue);

        // GET {id}/stock returns one row per product with its WAC and value.
        var stock = await _client.GetAsync<WarehouseStockItemDto[]>($"{GetUrl(warehouse.Id)}/stock");
        Assert.Equal(2, stock.Length);

        var rowA = Assert.Single(stock, x => x.ProductId == productA.Id);
        Assert.Equal(productA.Name, rowA.ProductName);
        Assert.Equal(productA.SKU, rowA.Sku);
        Assert.Equal(10, rowA.Quantity);
        Assert.Equal(100m, rowA.AverageCost);
        Assert.Equal(1_000m, rowA.Value);

        var rowB = Assert.Single(stock, x => x.ProductId == productB.Id);
        Assert.Equal(5, rowB.Quantity);
        Assert.Equal(200m, rowB.AverageCost);
        Assert.Equal(1_000m, rowB.Value);
    }

    [Fact]
    public async Task GetStock_ShouldReturnEmpty_WhenWarehouseHasNoStock()
    {
        // Arrange
        var warehouse = await CreateWarehouseAsync();

        // Act
        var stock = await _client.GetAsync<WarehouseStockItemDto[]>($"{GetUrl(warehouse.Id)}/stock");

        // Assert
        Assert.Empty(stock);
    }
}
