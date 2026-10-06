using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests
{
    // scope-9: every line snapshots its unit cost in the same write that moves the stock, so a past period's profit
    // never moves when prices change later.

    [Fact]
    public async Task CreateAsync_Sale_ShouldSnapshotTheWarehouseWac()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10, averageCost: 70m);

        var sale = await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, quantity: 3m, unitPrice: 100m));

        var line = Assert.Single((await GetDetailAsync(sale.Id)).Lines);
        Assert.Equal(70m, line.UnitCost);
        Assert.Equal(210m, line.LineCost);
        Assert.False(line.CostIsEstimated);
    }

    [Fact]
    public async Task CreateAsync_LaterSupplyAtAnotherPrice_ShouldNotChangeAPastSaleCost()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10, averageCost: 50m);
        var sale = await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, quantity: 4m, unitPrice: 100m));

        // 6 left @ 50, then 6 @ 200 → WAC 125.
        await PostTransactionAsync(Document(TransactionType.Supply, partnerId, productId, warehouseId, quantity: 6m, unitPrice: 200m));

        Assert.Equal(125m, (await ItemAsync(warehouseId, productId)).AverageCost);
        var line = Assert.Single((await GetDetailAsync(sale.Id)).Lines);
        Assert.Equal(50m, line.UnitCost);
        Assert.Equal(200m, line.LineCost);
    }

    [Fact]
    public async Task CreateAsync_SaleRefund_ShouldRestockAtTheOriginalSaleCost()
    {
        // Sold 4 @ cost 50; a supply of 6 @ 110 lifts the WAC to 80; 2 come back at their own cost 50.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10, averageCost: 50m);
        var sale = await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, quantity: 4m, unitPrice: 100m));
        await PostTransactionAsync(Document(TransactionType.Supply, partnerId, productId, warehouseId, quantity: 6m, unitPrice: 110m));
        Assert.Equal(80m, (await ItemAsync(warehouseId, productId)).AverageCost);

        var refund = await PostTransactionAsync(
            Document(TransactionType.SaleRefund, partnerId, productId, warehouseId, quantity: 2m, unitPrice: 100m) with
            {
                OriginalTransactionId = sale.Id,
                RefundReason = "Returned",
            });

        var line = Assert.Single((await GetDetailAsync(refund.Id)).Lines);
        Assert.Equal(50m, line.UnitCost);
        Assert.Equal(100m, line.LineCost);
        Assert.False(line.CostIsEstimated);

        // (12 × 80 + 2 × 50) / 14 = 75.714… → stored 75.71.
        var item = await ItemAsync(warehouseId, productId);
        Assert.Equal(14m, item.Quantity);
        Assert.Equal(75.71m, item.AverageCost);
    }

    [Fact]
    public async Task CreateAsync_SaleRefund_IntoAWarehouseWithoutTheProduct_ShouldCarryTheOriginalCost()
    {
        // backend-13 remainder: such a refund used to create the stock row at cost 0.
        var partnerId = await CreatePartnerAsync();
        var soldFrom = await CreateWarehouseAsync();
        var returnedTo = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(soldFrom, productId, quantity: 10, averageCost: 60m);
        var sale = await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, soldFrom, quantity: 3m, unitPrice: 100m));

        await PostTransactionAsync(
            Document(TransactionType.SaleRefund, partnerId, productId, returnedTo, quantity: 1m, unitPrice: 100m) with
            {
                OriginalTransactionId = sale.Id,
                RefundReason = "Returned",
            });

        var item = await ItemAsync(returnedTo, productId);
        Assert.Equal(1m, item.Quantity);
        Assert.Equal(60m, item.AverageCost);
    }

    [Fact]
    public async Task CreateAsync_SaleRefundOfAnEstimatedCost_ShouldBeEstimatedToo()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10, averageCost: 40m);
        var sale = await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, quantity: 2m, unitPrice: 100m));

        // As the backfill leaves a pre-2026-10-04 sale: an estimated cost.
        var soldLine = await _context.TransactionLines.AsTracking().FirstAsync(l => l.TransactionId == sale.Id);
        soldLine.UnitCost = 45m;
        soldLine.CostIsEstimated = true;
        await _context.SaveChangesAsync();

        var refund = await PostTransactionAsync(
            Document(TransactionType.SaleRefund, partnerId, productId, warehouseId, quantity: 1m, unitPrice: 100m) with
            {
                OriginalTransactionId = sale.Id,
                RefundReason = "Returned",
            });

        var line = Assert.Single((await GetDetailAsync(refund.Id)).Lines);
        Assert.Equal(45m, line.UnitCost);
        Assert.True(line.CostIsEstimated);
    }

    [Fact]
    public async Task CreateAsync_Supply_ShouldStoreItsNetPurchaseCost_AndEnterTheWacAtIt()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();

        var supply = await PostTransactionAsync(
            Document(TransactionType.Supply, partnerId, productId, warehouseId, quantity: 10m, unitPrice: 100m, discount: 10m));

        var line = Assert.Single((await GetDetailAsync(supply.Id)).Lines);
        Assert.Equal(90m, line.UnitCost);
        Assert.Equal(900m, line.LineCost);
        Assert.Equal(90m, (await ItemAsync(warehouseId, productId)).AverageCost);
    }

    [Fact]
    public async Task CreateAsync_SupplyRefund_ShouldStoreTheWacItLeavesAt()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10, averageCost: 30m);
        var supply = await PostTransactionAsync(Document(TransactionType.Supply, partnerId, productId, warehouseId, quantity: 10m, unitPrice: 50m));

        var refund = await PostTransactionAsync(
            Document(TransactionType.SupplyRefund, partnerId, productId, warehouseId, quantity: 4m, unitPrice: 50m) with
            {
                OriginalTransactionId = supply.Id,
                RefundReason = "Defective",
            });

        // (10 × 30 + 10 × 50) / 20 = 40; a stock-out leaves the WAC unchanged.
        var line = Assert.Single((await GetDetailAsync(refund.Id)).Lines);
        Assert.Equal(40m, line.UnitCost);
        Assert.Equal(160m, line.LineCost);
        Assert.Equal(40m, (await ItemAsync(warehouseId, productId)).AverageCost);
    }

    private Task<TransactionDetailDto> GetDetailAsync(int id) => _client.GetAsync<TransactionDetailDto>(GetUrl(id));

    private Task<Domain.Entities.WarehouseItem> ItemAsync(int warehouseId, int productId) =>
        _context.WarehouseItems.AsNoTracking().FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);

    private static CreateTransactionRequest Document(
        TransactionType type, int partnerId, int productId, int warehouseId, decimal quantity, decimal unitPrice, decimal discount = 0m)
        => new(
            PartnerId: partnerId,
            Type: type,
            Notes: null,
            Lines: [new CreateTransactionLine(productId, unitPrice, discount, DiscountType.Percentage, quantity)],
            WalletId: null,
            PaidAmount: 0m,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: null!,
            WarehouseId: warehouseId);
}
