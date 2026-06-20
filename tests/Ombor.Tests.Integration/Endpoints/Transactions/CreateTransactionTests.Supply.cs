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
    // A Supply is Expense: while unpaid we owe the partner, so the balance Total is negative.

    [Fact]
    public async Task CreateAsync_ShouldCloseSupply_WhenPaidInFull()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var inventoryId = await CreateInventoryAsync();
        var productId = await CreateProductAsync();

        var request = TransactionRequestFactory.Supply(partnerId, productId, inventoryId, due: 10_000m, walletId, paidAmount: 10_000m);

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
    [InlineData(10_000, 4_000, -6_000)]
    [InlineData(10_000, 7_500, -2_500)]
    public async Task CreateAsync_ShouldLeavePayable_WhenUnderpaid(decimal due, decimal paid, decimal expectedBalance)
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var inventoryId = await CreateInventoryAsync();
        var productId = await CreateProductAsync();

        var request = TransactionRequestFactory.Supply(partnerId, productId, inventoryId, due, walletId, paid);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(expectedBalance, balance.Total);

        var transaction = await _context.Transactions.FirstAsync(t => t.Id == created.Id);
        Assert.Equal(TransactionStatus.PartiallyPaid, transaction.Status);
        Assert.Equal(due - paid, transaction.UnpaidAmount);
    }

    [Fact]
    public async Task CreateAsync_ShouldLeaveFullPayable_WhenNothingPaid()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var inventoryId = await CreateInventoryAsync();
        var productId = await CreateProductAsync();

        var request = TransactionRequestFactory.Supply(partnerId, productId, inventoryId, due: 10_000m, walletId: null, paidAmount: 0m);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(-10_000m, balance.Total);

        var transaction = await _context.Transactions.FirstAsync(t => t.Id == created.Id);
        Assert.Equal(TransactionStatus.Open, transaction.Status);
        Assert.False(await _context.PaymentAllocations.AnyAsync(a => a.TransactionId == created.Id));
    }

    [Fact]
    public async Task CreateAsync_ShouldParkCompanyAdvance_WhenOverpaidAsAdvance()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var inventoryId = await CreateInventoryAsync();
        var productId = await CreateProductAsync();

        var request = TransactionRequestFactory.Supply(
            partnerId, productId, inventoryId, due: 10_000m, walletId, paidAmount: 15_000m, OverpaymentHandling.Advance);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert — we prepaid the partner, so they now owe us: the balance goes positive.
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(5_000m, balance.Total);

        var payment = await _context.Payments
            .Include(p => p.Allocations)
            .Include(p => p.Components)
            .SingleAsync(p => p.Allocations.Any(a => a.TransactionId == created.Id));

        var settlement = payment.Allocations.Single(a => a.Type == PaymentAllocationType.TransactionSettlement);
        Assert.Equal(10_000m, settlement.Amount);
        var advance = payment.Allocations.Single(a => a.Type == PaymentAllocationType.AdvanceCredit);
        Assert.Equal(5_000m, advance.Amount);

        var source = Assert.Single(payment.Components);
        Assert.Equal(15_000m, source.Amount);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnChange_WhenSupplyOverpaidAsChange()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var inventoryId = await CreateInventoryAsync();
        var productId = await CreateProductAsync();

        var request = TransactionRequestFactory.Supply(
            partnerId, productId, inventoryId, due: 10_000m, walletId, paidAmount: 15_000m, OverpaymentHandling.Change);

        // Act
        var created = await PostTransactionAsync(request);

        // Assert — change is excluded from balances (rule 15), so the balance settles to zero.
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(0m, balance.Total);

        var payment = await _context.Payments
            .Include(p => p.Allocations)
            .Include(p => p.Components)
            .SingleAsync(p => p.Allocations.Any(a => a.TransactionId == created.Id));

        var change = payment.Allocations.Single(a => a.Type == PaymentAllocationType.ChangeReturn);
        Assert.Equal(5_000m, change.Amount);

        var source = Assert.Single(payment.Components);
        Assert.Equal(10_000m, source.Amount);
    }

    [Fact]
    public async Task CreateAsync_ShouldReject_WhenSupplyAdvanceRequestedWithOutstandingDebt()
    {
        // Arrange — partner already has an open payable Supply.
        var partnerId = await CreatePartnerAsync();
        await CreateOpenTransactionAsync(partnerId, due: 10_000m, paid: 0m, TransactionType.Supply);

        var walletId = await CreateWalletAsync();
        var inventoryId = await CreateInventoryAsync();
        var productId = await CreateProductAsync();

        var request = TransactionRequestFactory.Supply(
            partnerId, productId, inventoryId, due: 5_000m, walletId, paidAmount: 8_000m, OverpaymentHandling.Advance);

        // Act + Assert
        await PostTransactionExpectingBadRequestAsync(request);
    }
}
