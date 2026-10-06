using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Tests.Common.Extensions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests
{
    [Fact]
    public async Task CreateAsync_ShouldRejectNegativeUnitPrice()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();

        // A negative price on a Supply would lower WAC (possibly below zero) and make a negative payable.
        var request = SaleLine(partnerId, productId, warehouseId, quantity: 1m, unitPrice: -100m, discount: 0m, DiscountType.Fixed)
            with { Type = TransactionType.Supply };
        var problem = await _client.PostAsync<JObject>(Routes.Transaction, request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.NotNull(problem["errors"]?["Lines[0].UnitPrice"]);
    }

    [Fact]
    public async Task CreateAsync_ShouldAcceptZeroUnitPrice_ForAFreeItem()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10);

        var sale = await PostTransactionAsync(SaleLine(partnerId, productId, warehouseId, quantity: 1m, unitPrice: 0m, discount: 0m, DiscountType.Fixed));

        Assert.Equal(0m, sale.TotalDue);
        // Nothing to pay means nothing owed: a free document is settled, never «Не оплачено».
        Assert.Equal("Closed", sale.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldNameProductAndQuantities_WhenStockIsInsufficient()
    {
        // backend-23 / hard rule 3: the block says which product and how much is there vs how much was asked.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 3);
        var productName = await _context.Products.Where(p => p.Id == productId).Select(p => p.Name).FirstAsync();

        var request = SaleLine(partnerId, productId, warehouseId, quantity: 5m, unitPrice: 1_000m, discount: 0m, DiscountType.Fixed);
        var problem = await _client.PostAsync<JObject>(Routes.Transaction, request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.Equal("stock.insufficient", (string?)problem["code"]);
        Assert.Equal(productName, (string?)problem["params"]?["productName"]);
        Assert.Equal(3m, (decimal?)problem["params"]?["available"]);
        Assert.Equal(5m, (decimal?)problem["params"]?["requested"]);
        Assert.NotNull(problem["errors"]?["Lines[0].Quantity"]);

        var stock = await _context.WarehouseItems.AsNoTracking().FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(3m, stock.Quantity);
    }
}
