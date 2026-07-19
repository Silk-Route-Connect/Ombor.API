using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests
{
    // Rule 5: cumulative refunded quantity per original line must not exceed the original quantity. The guard
    // must aggregate the request's own lines — two lines of the same product each pass against the same
    // baseline, so without pre-grouping their sum over-refunds.

    [Fact]
    public async Task CreateAsync_ShouldRejectRefund_WhenDuplicateProductLinesExceedOriginalQuantity()
    {
        // Arrange — a Sale of 10 units.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var sale = await PostTransactionAsync(BuildSale(partnerId, productId, warehouseId, quantity: 10m));

        // A SaleRefund naming the same product twice at 6 + 6 = 12 > 10. Each line passes 0+6 <= 10 on its own.
        var refund = BuildRefund(partnerId, productId, warehouseId, sale.Id, quantities: [6m, 6m]);

        // Act + Assert — must be a clean 400, no over-refund / over-restock.
        await PostTransactionExpectingBadRequestAsync(refund);
    }

    [Fact]
    public async Task CreateAsync_ShouldAcceptRefund_WhenDuplicateProductLinesSumWithinOriginalQuantity()
    {
        // Arrange — a Sale of 10 units.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var sale = await PostTransactionAsync(BuildSale(partnerId, productId, warehouseId, quantity: 10m));

        // Same product split across two lines summing to exactly the original quantity (6 + 4 = 10).
        var refund = BuildRefund(partnerId, productId, warehouseId, sale.Id, quantities: [6m, 4m]);

        // Act + Assert — the grouping must sum lines, not reject a legitimate split.
        await PostTransactionAsync(refund);
    }

    private static CreateTransactionRequest BuildSale(int partnerId, int productId, int warehouseId, decimal quantity)
        => new(
            PartnerId: partnerId,
            Type: TransactionType.Sale,
            Notes: null,
            Lines: [new CreateTransactionLine(productId, UnitPrice: 1_000m, Discount: 0m, DiscountType.Percentage, quantity)],
            WalletId: null,
            PaidAmount: 0m,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: null!,
            WarehouseId: warehouseId);

    private static CreateTransactionRequest BuildRefund(int partnerId, int productId, int warehouseId, int originalTransactionId, decimal[] quantities)
        => new(
            PartnerId: partnerId,
            Type: TransactionType.SaleRefund,
            Notes: null,
            Lines: [.. quantities.Select(q => new CreateTransactionLine(productId, UnitPrice: 1_000m, Discount: 0m, DiscountType.Percentage, q))],
            WalletId: null,
            PaidAmount: 0m,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: null!,
            WarehouseId: warehouseId,
            OriginalTransactionId: originalTransactionId,
            RefundReason: "Returned");
}
