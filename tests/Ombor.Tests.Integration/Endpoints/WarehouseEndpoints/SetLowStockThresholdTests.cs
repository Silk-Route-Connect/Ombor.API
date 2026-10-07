using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

/// <summary>
/// PUT /api/warehouses/{id}/stock/{productId}/threshold (DR-41): sets or clears one stock row's low-stock threshold and
/// serves the row back; a setting on the row, never a stock movement.
/// </summary>
public sealed class SetLowStockThresholdTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : WarehouseLowStockTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task SetThreshold_SetsAndClearsTheRowsThreshold_AndServesTheRecomputedRow()
    {
        var warehouse = await CreateWarehouseAsync();
        var productId = await StockAsync(warehouse.Id, quantity: 5m, threshold: null);

        var set = await PutThresholdAsync(warehouse.Id, productId, 10m);

        // The Activity Log reads it as a stock row change with no document whose only field is the threshold — the
        // client words a threshold-only change apart from a quantity change by that (collection tests run one at a
        // time, so the newest operation is this one).
        var activity = await _client.GetAsync<JObject>("activity?pageSize=1");
        var operation = activity["items"]![0]!;
        Assert.Equal("StockChanged", (string?)operation["kind"]);
        var change = Assert.Single(operation["changes"]!);
        Assert.Equal("Stock", (string?)change["entityKind"]);
        var field = Assert.Single(change["fields"]!);
        Assert.Equal("lowStockThreshold", (string?)field["field"]);
        Assert.Equal(10m, (decimal?)field["new"]);

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
    [InlineData("{}")]
    [InlineData("{\"lowStockThreshold\":null}")]
    public async Task SetThreshold_ClearsIt_WhenTheBodyHasNoThreshold_AndOmitsItFromTheRow(string body)
    {
        var warehouse = await CreateWarehouseAsync();
        var productId = await StockAsync(warehouse.Id, quantity: 2m, threshold: 10m);

        var row = await _client.PutAsync<JObject>(
            ThresholdUrl(warehouse.Id, productId), new StringContent(body, Encoding.UTF8, "application/json"), HttpStatusCode.OK);

        Assert.False(row.ContainsKey("lowStockThreshold"));
        Assert.False((bool)row["isLowStock"]!);
        Assert.Null(await ThresholdInDbAsync(warehouse.Id, productId));
        Assert.Equal(0, (await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id))).LowStockCount);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.2345")]
    [InlineData("1000000000000000")]
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
}
