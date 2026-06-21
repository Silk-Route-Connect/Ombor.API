using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Factories;
using PaymentAllocationType = Ombor.Domain.Enums.PaymentAllocationType;
using TransactionStatus = Ombor.Domain.Enums.TransactionStatus;
using TransactionType = Ombor.Domain.Enums.TransactionType;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests
{
    // A Sale is Income: while unpaid the partner owes us, so the balance Total is positive.

    [Fact]
    public async Task CreateAsync_ShouldCloseSale_WhenPaidInFull()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var request = TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 10_000m, walletId, paidAmount: 10_000m);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(0m, balance.Total);

        var transaction = await _context.Transactions
            .Include(x => x.PaymentAllocations)
            .SingleAsync(t => t.Id == created.Id);
        Assert.Equal(TransactionStatus.Closed, transaction.Status);
        var allocation = Assert.Single(transaction.PaymentAllocations);
        Assert.Equal(PaymentAllocationType.TransactionSettlement, allocation.Type);
        Assert.Equal(10_000m, allocation.Amount);
    }

    [Theory]
    [InlineData(10_000, 4_000, 6_000)]
    [InlineData(10_000, 7_500, 2_500)]
    public async Task CreateAsync_ShouldLeaveReceivable_WhenUnderpaid(decimal due, decimal paid, decimal expectedDebt)
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var request = TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due, walletId, paid);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(expectedDebt, balance.Total);

        var transaction = await _context.Transactions.FirstAsync(t => t.Id == created.Id);
        Assert.Equal(TransactionStatus.PartiallyPaid, transaction.Status);
        Assert.Equal(due - paid, transaction.UnpaidAmount);
    }

    [Fact]
    public async Task CreateAsync_ShouldLeaveFullReceivable_WhenNothingPaid()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var request = TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 10_000m, walletId: null, paidAmount: 0m);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(10_000m, balance.Total);

        var transaction = await _context.Transactions.FirstAsync(t => t.Id == created.Id);
        Assert.Equal(TransactionStatus.Open, transaction.Status);
        Assert.False(await _context.PaymentAllocations.AnyAsync(a => a.TransactionId == created.Id));
    }

    [Fact]
    public async Task CreateAsync_ShouldParkAdvance_WhenOverpaidAsAdvance()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var request = TransactionRequestFactory.Sale(
            partnerId, productId, warehouseId, due: 10_000m, walletId, paidAmount: 15_000m, OverpaymentHandling.Advance);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert — the parked advance is money we now owe the partner, so the balance goes negative.
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(-5_000m, balance.Total);

        var payment = await _context.Payments
            .Include(p => p.Allocations)
            .Include(p => p.Components)
            .SingleAsync(p => p.Allocations.Any(a => a.TransactionId == created.Id));

        var settlement = payment.Allocations.Single(a => a.Type == PaymentAllocationType.TransactionSettlement);
        Assert.Equal(10_000m, settlement.Amount);
        var advance = payment.Allocations.Single(a => a.Type == PaymentAllocationType.AdvanceCredit);
        Assert.Equal(5_000m, advance.Amount);

        // Rule 8: the wallet source equals the settling allocations (settlement + advance).
        var source = Assert.Single(payment.Components);
        Assert.Equal(15_000m, source.Amount);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnChange_WhenOverpaidAsChange()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        var request = TransactionRequestFactory.Sale(
            partnerId, productId, warehouseId, due: 10_000m, walletId, paidAmount: 15_000m, OverpaymentHandling.Change);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert — change is a memo, excluded from balances (rule 15), so the balance settles to zero.
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(0m, balance.Total);

        var payment = await _context.Payments
            .Include(p => p.Allocations)
            .Include(p => p.Components)
            .SingleAsync(p => p.Allocations.Any(a => a.TransactionId == created.Id));

        var change = payment.Allocations.Single(a => a.Type == PaymentAllocationType.ChangeReturn);
        Assert.Equal(5_000m, change.Amount);

        // Rule 15: the wallet only nets the kept amount, change handed back excluded.
        var source = Assert.Single(payment.Components);
        Assert.Equal(10_000m, source.Amount);
    }

    [Fact]
    public async Task CreateAsync_ShouldReject_WhenAdvanceRequestedWithOutstandingDebt()
    {
        // Arrange — partner already has an open receivable Sale.
        var partnerId = await CreatePartnerAsync();
        await CreateOpenTransactionAsync(partnerId, due: 10_000m, paid: 0m, TransactionType.Sale);

        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        // Pay this 5k Sale and try to park the rest as advance without clearing the 10k debt → rule 40.
        var request = TransactionRequestFactory.Sale(
            partnerId, productId, warehouseId, due: 5_000m, walletId, paidAmount: 8_000m, OverpaymentHandling.Advance);

        // Act + Assert
        await PostTransactionExpectingBadRequestAsync(request);
    }

    [Fact]
    public async Task CreateAsync_ShouldSettleOtherOpenSale_WhenSettlementsProvided()
    {
        // Arrange — an open receivable Sale to settle alongside the new one.
        var partnerId = await CreatePartnerAsync();
        var openSaleId = await CreateOpenTransactionAsync(partnerId, due: 10_000m, paid: 0m, TransactionType.Sale);

        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);

        // Pay 15k: 5k closes this Sale, 10k settles the open one (excess 0).
        var request = TransactionRequestFactory.Sale(
            partnerId, productId, warehouseId, due: 5_000m, walletId, paidAmount: 15_000m,
            OverpaymentHandling.Change,
            settlements: [new SettlementInput(openSaleId, 10_000m)]);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(0m, balance.Total);

        var newSale = await _context.Transactions.FirstAsync(t => t.Id == created.Id);
        Assert.Equal(TransactionStatus.Closed, newSale.Status);

        var openSale = await _context.Transactions.FirstAsync(t => t.Id == openSaleId);
        Assert.Equal(TransactionStatus.Closed, openSale.Status);
    }
}
