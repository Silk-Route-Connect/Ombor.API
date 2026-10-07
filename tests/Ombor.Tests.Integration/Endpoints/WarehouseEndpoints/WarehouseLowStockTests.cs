using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

/// <summary>
/// Low stock by warehouse-item threshold (DR-41): the «Остатки» rows serve their own threshold and flag, the warehouse
/// serves «Заканчивается» as a count of exactly the flagged rows, and a threshold is set on an opening-stock line or on
/// the row itself. Each test stocks a fresh warehouse with fresh products.
/// </summary>
public sealed class WarehouseLowStockTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WarehouseTestsBase(factory, outputHelper)
{
    private const int ForeignOrganizationId = 2;

    [Fact]
    public async Task Stock_ServesEachRowsThreshold_AndFlagsOnlyTrackedActiveRowsAtOrBelowIt()
    {
        var warehouse = await CreateWarehouseAsync();
        var plenty = await StockAsync(warehouse.Id, quantity: 20m, threshold: 10m);
        var scarce = await StockAsync(warehouse.Id, quantity: 5m, threshold: 10m);
        var untrackedEmpty = await StockAsync(warehouse.Id, quantity: 4m, threshold: null, sellOut: true);
        var trackedEmpty = await StockAsync(warehouse.Id, quantity: 4m, threshold: 3m, sellOut: true);
        var archived = await StockAsync(warehouse.Id, quantity: 2m, threshold: 10m, archivedProduct: true);

        var stock = await _client.GetAsync<WarehouseStockItemDto[]>($"{GetUrl(warehouse.Id)}/stock");

        Assert.Equal(5, stock.Length);
        AssertRow(stock, plenty, threshold: 10m, isLow: false);
        AssertRow(stock, scarce, threshold: 10m, isLow: true);
        AssertRow(stock, untrackedEmpty, threshold: null, isLow: false);
        AssertRow(stock, trackedEmpty, threshold: 3m, isLow: true);
        AssertRow(stock, archived, threshold: 10m, isLow: false);
        Assert.Equal(0m, Assert.Single(stock, r => r.ProductId == untrackedEmpty).Quantity);

        // «Товаров» counts products on hand (the two emptied rows are not); «Заканчивается» counts the flagged rows.
        var fetched = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id));
        Assert.Equal(3, fetched.ProductCount);
        Assert.Equal(2, fetched.LowStockCount);

        var listed = Assert.Single(await _client.GetAsync<WarehouseDto[]>(GetUrl()), w => w.Id == warehouse.Id);
        Assert.Equal(3, listed.ProductCount);
        Assert.Equal(2, listed.LowStockCount);
    }

    [Fact]
    public async Task ArchivedWarehouse_CountsNothingLow_ButKeepsItsThresholds()
    {
        var warehouse = await CreateWarehouseAsync();
        var scarce = await StockAsync(warehouse.Id, quantity: 2m, threshold: 10m);

        var archived = await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/archive", content: new { }, HttpStatusCode.OK);
        var archivedStock = await _client.GetAsync<WarehouseStockItemDto[]>($"{GetUrl(warehouse.Id)}/stock");

        Assert.Equal(0, archived.LowStockCount);
        AssertRow(archivedStock, scarce, threshold: 10m, isLow: false);

        var restored = await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/restore", content: new { }, HttpStatusCode.OK);

        Assert.Equal(1, restored.LowStockCount);
    }

    [Fact]
    public async Task SetThreshold_SetsAndClearsTheRowsThreshold_AndServesTheRecomputedRow()
    {
        var warehouse = await CreateWarehouseAsync();
        var productId = await StockAsync(warehouse.Id, quantity: 5m, threshold: null);

        var set = await PutThresholdAsync(warehouse.Id, productId, 10m);

        Assert.Equal(productId, set.ProductId);
        Assert.Equal(5m, set.Quantity);
        Assert.Equal(10m, set.LowStockThreshold);
        Assert.True(set.IsLowStock);
        Assert.Equal(1, (await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id))).LowStockCount);

        var fractional = await PutThresholdAsync(warehouse.Id, productId, 4.5m);

        Assert.Equal(4.5m, fractional.LowStockThreshold);
        Assert.False(fractional.IsLowStock);

        var cleared = await PutThresholdAsync(warehouse.Id, productId, null);

        Assert.Null(cleared.LowStockThreshold);
        Assert.False(cleared.IsLowStock);
        Assert.Equal(0, (await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id))).LowStockCount);

        // A setting on the row, not a stock movement: the quantity is untouched and the change is audited.
        var item = await _context.WarehouseItems.SingleAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == productId);
        Assert.Equal(5m, item.Quantity);
        var audited = await _context.AuditEntries
            .Where(a => a.EntityType == nameof(WarehouseItem) && a.EntityId == item.Id && a.Action == AuditAction.Updated)
            .Select(a => a.NewValues)
            .ToArrayAsync();
        Assert.Equal(3, audited.Length);
        Assert.All(audited, values => Assert.Contains(nameof(WarehouseItem.LowStockThreshold), values));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.2345")]
    public async Task SetThreshold_Returns400_WhenTheThresholdIsNegativeOrTooPrecise(string threshold)
    {
        var warehouse = await CreateWarehouseAsync();
        var productId = await StockAsync(warehouse.Id, quantity: 5m, threshold: 3m);

        var problem = await _client.PutAsync<ValidationProblemDetails>(
            ThresholdUrl(warehouse.Id, productId),
            new { lowStockThreshold = decimal.Parse(threshold, CultureInfo.InvariantCulture) },
            HttpStatusCode.BadRequest);

        Assert.Contains(nameof(SetLowStockThresholdRequest.LowStockThreshold), problem.Errors.Keys);
        Assert.Equal(3m, await ThresholdInDbAsync(warehouse.Id, productId));
    }

    [Fact]
    public async Task SetThreshold_Returns404_ForAnUnknownWarehouse_OrAProductWithoutAStockRowThere()
    {
        var warehouse = await CreateWarehouseAsync();
        var otherWarehouse = await CreateWarehouseAsync();
        var stockedElsewhere = await StockAsync(otherWarehouse.Id, quantity: 5m, threshold: null);
        var neverStocked = await CreateProductAsync();

        await _client.PutAsync(ThresholdUrl(NonExistentEntityId, stockedElsewhere), new { lowStockThreshold = 1m }, HttpStatusCode.NotFound);
        await _client.PutAsync(ThresholdUrl(warehouse.Id, stockedElsewhere), new { lowStockThreshold = 1m }, HttpStatusCode.NotFound);
        await _client.PutAsync(ThresholdUrl(warehouse.Id, neverStocked.Id), new { lowStockThreshold = 1m }, HttpStatusCode.NotFound);

        Assert.Null(await ThresholdInDbAsync(otherWarehouse.Id, stockedElsewhere));
    }

    [Fact]
    public async Task SetThreshold_Returns404_ForAnotherOrganizationsStockRow_AndLeavesItAlone()
    {
        int foreignWarehouseId;
        int foreignProductId;
        await using (var foreign = CreateContext(ForeignOrganizationId))
        {
            var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
            var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Samarkand" };
            foreign.Categories.Add(category);
            foreign.Warehouses.Add(warehouse);
            await foreign.SaveChangesAsync();

            var product = new Product
            {
                Name = $"Product {Guid.NewGuid():N}",
                SKU = $"SKU-{Guid.NewGuid():N}",
                SalePrice = 100m,
                SupplyPrice = 50m,
                Measurement = UnitOfMeasurement.Piece,
                Type = ProductType.All,
                CategoryId = category.Id,
                Category = null!,
            };
            foreign.Products.Add(product);
            await foreign.SaveChangesAsync();

            foreign.WarehouseItems.Add(new WarehouseItem
            {
                WarehouseId = warehouse.Id,
                ProductId = product.Id,
                Quantity = 1m,
                AverageCost = 10m,
                Warehouse = null!,
                Product = null!,
            });
            await foreign.SaveChangesAsync();

            (foreignWarehouseId, foreignProductId) = (warehouse.Id, product.Id);
        }

        await _client.PutAsync(ThresholdUrl(foreignWarehouseId, foreignProductId), new { lowStockThreshold = 5m }, HttpStatusCode.NotFound);

        await using var check = CreateContext(ForeignOrganizationId);
        var item = await check.WarehouseItems.SingleAsync(i => i.WarehouseId == foreignWarehouseId && i.ProductId == foreignProductId);
        Assert.Null(item.LowStockThreshold);
    }

    [Fact]
    public async Task SetThreshold_IsAllowedOnAnArchivedWarehouse_ButCountsOnlyOnceItIsRestored()
    {
        var warehouse = await CreateWarehouseAsync();
        var productId = await StockAsync(warehouse.Id, quantity: 2m, threshold: null);
        await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/archive", content: new { }, HttpStatusCode.OK);

        var row = await PutThresholdAsync(warehouse.Id, productId, 10m);

        Assert.Equal(10m, row.LowStockThreshold);
        Assert.False(row.IsLowStock);

        var restored = await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/restore", content: new { }, HttpStatusCode.OK);
        Assert.Equal(1, restored.LowStockCount);
    }

    [Fact]
    public async Task OpeningStock_SetsTheThresholdOfEachLineThatNamesOne_OthersStayUntracked()
    {
        var warehouse = await CreateWarehouseAsync();
        var tracked = await CreateProductAsync();
        var untracked = await CreateProductAsync();

        var response = await _client.PostAsync<WarehouseDto>(
            $"{GetUrl(warehouse.Id)}/opening-stock",
            new AddOpeningStockRequest(
                warehouse.Id,
                [
                    new OpeningStockLine(tracked.Id, Quantity: 5m, UnitCost: 10m, LowStockThreshold: 10m),
                    new OpeningStockLine(untracked.Id, Quantity: 3m, UnitCost: 10m),
                ]),
            HttpStatusCode.OK);

        Assert.Equal(2, response.ProductCount);
        Assert.Equal(1, response.LowStockCount);

        var stock = await _client.GetAsync<WarehouseStockItemDto[]>($"{GetUrl(warehouse.Id)}/stock");
        AssertRow(stock, tracked.Id, threshold: 10m, isLow: true);
        AssertRow(stock, untracked.Id, threshold: null, isLow: false);
    }

    [Fact]
    public async Task OpeningStock_RejectsANegativeThreshold_AndNeverTouchesAnExistingRow()
    {
        var warehouse = await CreateWarehouseAsync();
        var fresh = await CreateProductAsync();
        var stocked = await StockAsync(warehouse.Id, quantity: 5m, threshold: 7m);

        var negative = await _client.PostAsync<ValidationProblemDetails>(
            $"{GetUrl(warehouse.Id)}/opening-stock",
            new AddOpeningStockRequest(warehouse.Id, [new OpeningStockLine(fresh.Id, Quantity: 5m, UnitCost: 10m, LowStockThreshold: -1m)]),
            HttpStatusCode.BadRequest);
        Assert.Contains("Items[0].LowStockThreshold", negative.Errors.Keys);

        // A product already stocked is rejected whatever its line says, so an opening line can never change or clear an
        // existing row's threshold.
        await _client.PostAsync<ValidationProblemDetails>(
            $"{GetUrl(warehouse.Id)}/opening-stock",
            new AddOpeningStockRequest(warehouse.Id, [new OpeningStockLine(stocked, Quantity: 5m, UnitCost: 10m)]),
            HttpStatusCode.BadRequest);
        Assert.Equal(7m, await ThresholdInDbAsync(warehouse.Id, stocked));
        Assert.False(await _context.WarehouseItems.AnyAsync(i => i.WarehouseId == warehouse.Id && i.ProductId == fresh.Id));
    }

    private static void AssertRow(WarehouseStockItemDto[] stock, int productId, decimal? threshold, bool isLow)
    {
        var row = Assert.Single(stock, r => r.ProductId == productId);
        Assert.Equal(threshold, row.LowStockThreshold);
        Assert.Equal(isLow, row.IsLowStock);
    }

    private static string ThresholdUrl(int warehouseId, int productId) =>
        $"{Routes.Warehouse}/{warehouseId}/stock/{productId}/threshold";

    private Task<WarehouseStockItemDto> PutThresholdAsync(int warehouseId, int productId, decimal? threshold) =>
        _client.PutAsync<WarehouseStockItemDto>(
            ThresholdUrl(warehouseId, productId), new SetLowStockThresholdRequest(threshold), HttpStatusCode.OK);

    private Task<decimal?> ThresholdInDbAsync(int warehouseId, int productId) =>
        _context.WarehouseItems
            .Where(i => i.WarehouseId == warehouseId && i.ProductId == productId)
            .Select(i => i.LowStockThreshold)
            .SingleAsync();

    private ApplicationDbContext CreateContext(int organizationId) =>
        new(
            _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>(),
            new FakeOrganizationAccessor(organizationId));

    /// <summary>A fresh product stocked through opening stock, then given its threshold; written off to zero when asked.</summary>
    private async Task<int> StockAsync(
        int warehouseId, decimal quantity, decimal? threshold, bool sellOut = false, bool archivedProduct = false)
    {
        var product = await CreateProductAsync(archivedProduct);
        await _client.PostAsync<WarehouseDto>(
            $"{GetUrl(warehouseId)}/opening-stock",
            new AddOpeningStockRequest(warehouseId, [new OpeningStockLine(product.Id, quantity, UnitCost: 10m)]),
            HttpStatusCode.OK);

        if (sellOut)
        {
            await _client.PostAsync<StockAdjustmentDto>(
                Routes.StockAdjustment,
                new { warehouseId, productId = product.Id, direction = "Decrease", quantity, reason = "Damage", note = (string?)null });
        }

        await SetLowStockThresholdAsync(warehouseId, product.Id, threshold);

        return product.Id;
    }
}
