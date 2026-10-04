using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence.DataFixes;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

/// <summary>
/// Sale and sale-refund lines recorded before cost snapshots (scope-9) get the best available estimate, marked as
/// estimated: the WAC in the sale's warehouse, else the product's organization-wide average, else its supply price; a
/// refund line takes its sale's cost. Supply-side lines and lines that already carry a cost are left alone.
/// </summary>
public sealed class TransactionLineCostBackfillTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    [Fact]
    public async Task Backfill_ShouldEstimateFromTheSaleWarehouseWac_AndGiveTheRefundTheSaleCost()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 5, averageCost: 70m);
        var sale = await PlantAsync(partnerId, warehouseId, TransactionType.Sale, (productId, 2m, null));
        var refund = await PlantAsync(partnerId, warehouseId, TransactionType.SaleRefund, (productId, 1m, null), originalId: sale);

        await _context.Database.ExecuteSqlRawAsync(TransactionLineCostBackfill.Sql);

        var soldLine = await LineOfAsync(sale);
        Assert.Equal(70m, soldLine.UnitCost);
        Assert.True(soldLine.CostIsEstimated);

        var refundLine = await LineOfAsync(refund);
        Assert.Equal(70m, refundLine.UnitCost);
        Assert.True(refundLine.CostIsEstimated);
    }

    [Fact]
    public async Task Backfill_ShouldFallBackToTheOrganizationWideAverage_ThenTheSupplyPrice()
    {
        var partnerId = await CreatePartnerAsync();
        var soldFrom = await CreateWarehouseAsync();
        var elsewhere = await CreateWarehouseAsync();
        var other = await CreateWarehouseAsync();
        var stockedElsewhere = await CreateProductAsync();
        await SeedStockAsync(elsewhere, stockedElsewhere, quantity: 4, averageCost: 30m);
        await SeedStockAsync(other, stockedElsewhere, quantity: 6, averageCost: 40m);
        var neverStocked = await CreateProductAsync(); // supply price 50

        var first = await PlantAsync(partnerId, soldFrom, TransactionType.Sale, (stockedElsewhere, 1m, null));
        var second = await PlantAsync(partnerId, soldFrom, TransactionType.Sale, (neverStocked, 1m, null));

        await _context.Database.ExecuteSqlRawAsync(TransactionLineCostBackfill.Sql);

        Assert.Equal(36m, (await LineOfAsync(first)).UnitCost); // (4 × 30 + 6 × 40) / 10
        Assert.Equal(50m, (await LineOfAsync(second)).UnitCost);
    }

    [Fact]
    public async Task Backfill_ShouldLeaveSupplyLinesAndSnapshottedCostsAlone_AndBeIdempotent()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 5, averageCost: 70m);
        var supply = await PlantAsync(partnerId, warehouseId, TransactionType.Supply, (productId, 3m, null));
        var snapshotted = await PlantAsync(partnerId, warehouseId, TransactionType.Sale, (productId, 1m, 88m));
        await PlantAsync(partnerId, warehouseId, TransactionType.Sale, (productId, 1m, null));

        await _context.Database.ExecuteSqlRawAsync(TransactionLineCostBackfill.Sql);
        var secondRun = await _context.Database.ExecuteSqlRawAsync(TransactionLineCostBackfill.Sql);

        Assert.Equal(0, secondRun);
        Assert.Null((await LineOfAsync(supply)).UnitCost);
        var kept = await LineOfAsync(snapshotted);
        Assert.Equal(88m, kept.UnitCost);
        Assert.False(kept.CostIsEstimated);
    }

    private Task<TransactionLine> LineOfAsync(int transactionId) =>
        _context.TransactionLines.AsNoTracking().SingleAsync(l => l.TransactionId == transactionId);

    private async Task<int> PlantAsync(
        int partnerId, int warehouseId, TransactionType type, (int ProductId, decimal Quantity, decimal? UnitCost) line, int? originalId = null)
    {
        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            WarehouseId = warehouseId,
            Type = type,
            DateUtc = DateTimeOffset.UtcNow,
            Status = TransactionStatus.Open,
            TotalDue = line.Quantity * 100m,
            OriginalTransactionId = originalId,
            RefundReason = originalId is null ? null : "Returned",
            Lines =
            [
                new TransactionLine
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = 100m,
                    UnitCost = line.UnitCost,
                    Product = null!,
                    Transaction = null!,
                },
            ],
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction.Id;
    }
}
