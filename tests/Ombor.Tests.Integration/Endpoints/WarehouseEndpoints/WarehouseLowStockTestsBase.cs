using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

/// <summary>
/// Shared setup for the low-stock tests (DR-41): stocks fresh products into a warehouse, gives a row its threshold, and
/// reads a row back. Each test stocks a fresh warehouse with fresh products.
/// </summary>
public abstract class WarehouseLowStockTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WarehouseTestsBase(factory, outputHelper)
{
    protected const int ForeignOrganizationId = 2;

    protected static void AssertRow(WarehouseStockItemDto[] stock, int productId, decimal? threshold, bool isLow)
    {
        var row = Assert.Single(stock, r => r.ProductId == productId);
        Assert.Equal(threshold, row.LowStockThreshold);
        Assert.Equal(isLow, row.IsLowStock);
    }

    protected static string ThresholdUrl(int warehouseId, int productId) =>
        $"{Routes.Warehouse}/{warehouseId}/stock/{productId}/threshold";

    protected Task<WarehouseStockItemDto[]> GetStockAsync(int warehouseId) =>
        _client.GetAsync<WarehouseStockItemDto[]>($"{GetUrl(warehouseId)}/stock");

    protected Task<WarehouseStockItemDto> PutThresholdAsync(int warehouseId, int productId, decimal? threshold) =>
        _client.PutAsync<WarehouseStockItemDto>(
            ThresholdUrl(warehouseId, productId), new SetLowStockThresholdRequest(threshold), HttpStatusCode.OK);

    protected Task<decimal?> ThresholdInDbAsync(int warehouseId, int productId) =>
        _context.WarehouseItems
            .Where(i => i.WarehouseId == warehouseId && i.ProductId == productId)
            .Select(i => i.LowStockThreshold)
            .SingleAsync();

    private protected ApplicationDbContext CreateContext(int organizationId) =>
        new(
            _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>(),
            new FakeOrganizationAccessor(organizationId));

    /// <summary>A fresh product stocked through opening stock, then given its threshold; written off to zero when asked.</summary>
    protected async Task<int> StockAsync(
        int warehouseId, decimal quantity, decimal? threshold, bool sellOut = false, bool archivedProduct = false)
    {
        var product = await CreateProductAsync(archivedProduct);
        await _client.PostAsync<WarehouseDto>(
            $"{GetUrl(warehouseId)}/opening-stock",
            new AddOpeningStockRequest(warehouseId, [new OpeningStockLine(product.Id, quantity, UnitCost: 10m)]),
            HttpStatusCode.OK);

        if (sellOut)
        {
            await WriteOffAsync(warehouseId, product.Id, quantity);
        }

        await SetLowStockThresholdAsync(warehouseId, product.Id, threshold);

        return product.Id;
    }

    protected Task<StockAdjustmentDto> WriteOffAsync(int warehouseId, int productId, decimal quantity) =>
        _client.PostAsync<StockAdjustmentDto>(
            Routes.StockAdjustment,
            new { warehouseId, productId, direction = "Decrease", quantity, reason = "Damage", note = (string?)null });
}
