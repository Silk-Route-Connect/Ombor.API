using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.MovementEndpoints;

// Stock quantity is decimal (weight-measured products, e.g. kg). These prove fractional quantities flow through
// the shared stock path — WAC recompute, transfers — without being truncated to whole units.
public sealed class FractionalQuantityTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : MovementTestsBase(factory, output)
{
    [Fact]
    public async Task WeightedAverageCost_RecomputesOnFractionalQuantities_WithoutTruncation()
    {
        // Arrange — open 2.5 kg @ 40, then supply 1.5 kg @ 60 through the real stock-in path.
        var warehouse = await CreateWarehouseAsync();
        var product = await CreateProductAsync();
        var partner = await CreatePartnerAsync();
        await AddOpeningStockAsync(warehouse, product, quantity: 2.5m, unitCost: 40m);

        var supply = new CreateTransactionRequest(
            PartnerId: partner,
            Type: TransactionType.Supply,
            Notes: null,
            Lines: [new CreateTransactionLine(ProductId: product, UnitPrice: 60m, Discount: 0m, DiscountType: DiscountType.Fixed, Quantity: 1.5m)],
            WalletId: null,
            PaidAmount: 0m,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: [],
            WarehouseId: warehouse);

        // Act
        await PostTransactionAsync(supply);

        // Assert — WAC = (2.5·40 + 1.5·60) / 4.0 = 190 / 4 = 47.5; quantity is the exact 4.0, not truncated.
        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouse && i.ProductId == product);
        Assert.Equal(4.0m, item.Quantity);
        Assert.Equal(47.5m, item.AverageCost);
    }

    [Fact]
    public async Task Transfer_MovesFractionalQuantity_AcrossWarehouses_WithoutTruncation()
    {
        // Arrange — 5 kg in the source.
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();
        var product = await CreateProductAsync();
        await AddOpeningStockAsync(source, product, quantity: 5m, unitCost: 10m);

        // Act — move 2.5 kg.
        await AddTransferAsync(source, destination, product, quantity: 2.5m);

        // Assert — both sides hold the exact fraction; total stock is conserved.
        var src = await _context.WarehouseItems.AsNoTracking().FirstAsync(i => i.WarehouseId == source && i.ProductId == product);
        var dst = await _context.WarehouseItems.AsNoTracking().FirstAsync(i => i.WarehouseId == destination && i.ProductId == product);
        Assert.Equal(2.5m, src.Quantity);
        Assert.Equal(2.5m, dst.Quantity);
    }

    [Fact]
    public async Task SaleAndTransferLines_ShouldStoreThreeDecimalQuantities_Exactly()
    {
        // backend-17: the line rows used to be decimal(18,2), so a 1.125 kg sale moved 1.125 out of stock but
        // recorded 1.13 — the movement ledger then no longer reconciled with the stock it explains.
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();
        var product = await CreateProductAsync();
        var partner = await CreatePartnerAsync();
        await AddOpeningStockAsync(source, product, quantity: 10m, unitCost: 40m);

        var saleId = await PostTransactionAsync(new CreateTransactionRequest(
            PartnerId: partner,
            Type: TransactionType.Sale,
            Notes: null,
            Lines: [new CreateTransactionLine(ProductId: product, UnitPrice: 100m, Discount: 0m, DiscountType: DiscountType.Fixed, Quantity: 1.125m)],
            WalletId: null,
            PaidAmount: 0m,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: [],
            WarehouseId: source));
        await AddTransferAsync(source, destination, product, quantity: 2.125m);

        var saleLine = await _context.TransactionLines.AsNoTracking().FirstAsync(l => l.TransactionId == saleId);
        var transferLine = await _context.TransferLines.AsNoTracking()
            .FirstAsync(l => l.ProductId == product);
        var src = await _context.WarehouseItems.AsNoTracking().FirstAsync(i => i.WarehouseId == source && i.ProductId == product);

        Assert.Equal(1.125m, saleLine.Quantity);
        Assert.Equal(2.125m, transferLine.Quantity);
        Assert.Equal(10m - 1.125m - 2.125m, src.Quantity);
    }
}
