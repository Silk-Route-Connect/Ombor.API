using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Tests.Common.Extensions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests
{
    // backend-13: a refund belongs to the original's partner and is priced from the original line (net of its
    // discount) — the request's prices are ignored, so a refund can never pay out more than was charged.

    [Fact]
    public async Task CreateAsync_ShouldRejectRefund_BookedToAnotherPartner()
    {
        var buyer = await CreatePartnerAsync();
        var someoneElse = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);
        var sale = await PostTransactionAsync(BuildSale(buyer, productId, warehouseId, quantity: 2m));

        var refund = BuildRefund(someoneElse, productId, warehouseId, sale.Id, quantities: [1m]);
        var problem = await _client.PostAsync<JObject>(Routes.Transaction, refund.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.NotNull(problem["errors"]?[nameof(CreateTransactionRequest.PartnerId)]);
        Assert.False(await _context.Transactions.AnyAsync(t => t.OriginalTransactionId == sale.Id));
    }

    [Fact]
    public async Task CreateAsync_ShouldPriceRefundFromOriginalPercentageLine_IgnoringRequestPrice()
    {
        // Sold 4 @ 1 000 with 10 % off → 900 per unit net.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);
        var sale = await PostTransactionAsync(SaleLine(partnerId, productId, warehouseId, quantity: 4m, unitPrice: 1_000m, discount: 10m, DiscountType.Percentage));

        // The request claims 50 000 per unit and no discount.
        var refund = await PostTransactionAsync(RefundLine(partnerId, productId, warehouseId, sale.Id, quantity: 2m, unitPrice: 50_000m));

        var line = Assert.Single(refund.Lines);
        Assert.Equal(1_000m, line.UnitPrice);
        Assert.Equal(10m, line.Discount);
        Assert.Equal("Percentage", line.DiscountType);
        Assert.Equal(1_800m, refund.TotalDue);
    }

    [Fact]
    public async Task CreateAsync_ShouldProRateOriginalFixedDiscount_OnPartialRefund()
    {
        // Sold 4 @ 1 000 with a 400 fixed discount on the whole line → 3 600, i.e. 900 per unit net.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);
        var sale = await PostTransactionAsync(SaleLine(partnerId, productId, warehouseId, quantity: 4m, unitPrice: 1_000m, discount: 400m, DiscountType.Fixed));

        var refund = await PostTransactionAsync(RefundLine(partnerId, productId, warehouseId, sale.Id, quantity: 1m, unitPrice: 0m));

        var line = Assert.Single(refund.Lines);
        Assert.Equal(1_000m, line.UnitPrice);
        Assert.Equal(100m, line.Discount);
        Assert.Equal(900m, refund.TotalDue);
    }

    private static CreateTransactionRequest SaleLine(
        int partnerId, int productId, int warehouseId, decimal quantity, decimal unitPrice, decimal discount, DiscountType discountType)
        => BuildSale(partnerId, productId, warehouseId, quantity) with
        {
            Lines = [new CreateTransactionLine(productId, unitPrice, discount, discountType, quantity)],
        };

    private static CreateTransactionRequest RefundLine(
        int partnerId, int productId, int warehouseId, int originalId, decimal quantity, decimal unitPrice)
        => BuildRefund(partnerId, productId, warehouseId, originalId, quantities: [quantity]) with
        {
            Lines = [new CreateTransactionLine(productId, unitPrice, 0m, DiscountType.Fixed, quantity)],
        };
}
