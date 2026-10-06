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

    [Fact]
    public async Task CreateAsync_ShouldRejectPackageRefund_WhenResolvedQuantityExceedsWhatWasSold()
    {
        // Sold one box of 12. The refund line claims base quantity 1 but enters 5 boxes; the server books
        // 5 × 12 = 60, so the rule-5 cap must count 60, not the ignored 1.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync(packageSize: 12);
        await SeedStockAsync(warehouseId, productId, quantity: 100);
        var sale = await PostTransactionAsync(BuildSale(partnerId, productId, warehouseId, quantity: 12m, packageQuantity: 1));

        var refund = PackageRefund(partnerId, productId, warehouseId, sale.Id, quantity: 1m, packageQuantity: 5);
        var problem = await _client.PostAsync<JObject>(Routes.Transaction, refund.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.NotNull(problem["errors"]?["Lines[0].Quantity"]);
        Assert.False(await _context.Transactions.AnyAsync(t => t.OriginalTransactionId == sale.Id));
        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(88m, item.Quantity);
    }

    [Fact]
    public async Task CreateAsync_ShouldCapPackageRefund_ByResolvedQuantity()
    {
        // Sold two boxes of 12 (24). Returning one box books 12 base units; the request's base quantity (999) is
        // ignored for a package entry by the cap exactly as it is by the stock move.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync(packageSize: 12);
        await SeedStockAsync(warehouseId, productId, quantity: 100);
        var sale = await PostTransactionAsync(BuildSale(partnerId, productId, warehouseId, quantity: 24m, packageQuantity: 2));

        var refund = await PostTransactionAsync(PackageRefund(partnerId, productId, warehouseId, sale.Id, quantity: 999m, packageQuantity: 1));

        Assert.Equal(12m, Assert.Single(refund.Lines).Quantity);
    }

    private static CreateTransactionRequest PackageRefund(
        int partnerId, int productId, int warehouseId, int originalId, decimal quantity, int packageQuantity)
        => BuildRefund(partnerId, productId, warehouseId, originalId, quantities: [quantity]) with
        {
            Lines = [new CreateTransactionLine(productId, 1_000m, 0m, DiscountType.Percentage, quantity, packageQuantity)],
        };

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
