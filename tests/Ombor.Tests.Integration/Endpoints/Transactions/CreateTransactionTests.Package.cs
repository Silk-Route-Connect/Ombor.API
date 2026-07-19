using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Tests.Common.Factories;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests
{
    // R21: a package-entry line is resolved to base units server-side from the product's package size (which
    // the client never supplies), and the size is snapshotted on the line for audit.

    [Fact]
    public async Task CreateAsync_ShouldComputeBaseQuantityFromPackageEntry_AndSnapshotProductSize()
    {
        // Arrange — a product packaged 12-to-a-box; a Sale entered as 2 boxes = 24 base units, from 100 on hand.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync(packageSize: 12);
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        // The client's base Quantity is deliberately wrong (999) to prove the server ignores it and recomputes
        // the base from the pack count × the product's authoritative size.
        var request = BuildSale(partnerId, productId, warehouseId, quantity: 999m, packageQuantity: 2);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert — server computed base Quantity = 2 × 12 and snapshotted the size; the client's 999 was ignored.
        var createdLine = Assert.Single(created.Lines);
        Assert.Equal(24m, createdLine.Quantity);
        Assert.Equal(12, createdLine.PackageSize);

        // The detail projection serves the same.
        var detail = await _client.GetAsync<TransactionDetailDto>(GetUrl(created.Id));
        var detailLine = Assert.Single(detail.Lines);
        Assert.Equal(24m, detailLine.Quantity);
        Assert.Equal(12, detailLine.PackageSize);

        // Stock moved by the computed base quantity (24), not the pack count.
        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        Assert.Equal(76m, item.Quantity);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectPackageEntry_WhenProductHasNoPackageSize()
    {
        // Arrange — a product with no configured package size (0) cannot be entered in packages.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var request = BuildSale(partnerId, productId, warehouseId, quantity: 24m, packageQuantity: 2);

        // Act + Assert — a clean 400: a package entry against an unpackaged product is rejected.
        await PostTransactionExpectingBadRequestAsync(request);
    }

    [Fact]
    public async Task CreateAsync_ShouldLeavePackageSizeNull_WhenEnteredInBaseUnits()
    {
        // Arrange — a base-unit Sale (no pack count) against a packaged product.
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync(packageSize: 12);
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var request = TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 5_000m, walletId: null, paidAmount: 0m);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert — no pack entry means no snapshot.
        Assert.Null(Assert.Single(created.Lines).PackageSize);
    }

    private static CreateTransactionRequest BuildSale(int partnerId, int productId, int warehouseId, decimal quantity, int? packageQuantity)
        => new(
            PartnerId: partnerId,
            Type: TransactionType.Sale,
            Notes: null,
            Lines: [new CreateTransactionLine(productId, UnitPrice: 1_000m, Discount: 0m, DiscountType.Percentage, quantity, packageQuantity)],
            WalletId: null,
            PaidAmount: 0m,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: null!,
            WarehouseId: warehouseId);
}
