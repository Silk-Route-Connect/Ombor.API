using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Requests.Payment;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Factories;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests
{
    public static readonly object[][] SupplyPaidInFullCases =
    [
        // openingBalance, due, cash, credit, expectedBalance
        [  0m, 10_000m, 10_000m, 0m, 0m ],
        [ -5_000m, 10_000m, 10_000m, 0m, -5_000m ], // debit unchanged
        [  5_000m, 10_000m, 10_000m, 0m, 5_000m ],  // credit unchanged
        [ -5_000m, 10_000m, 5_000m, 5_000m, 0m ] // advance consumed
    ];

    [Theory, MemberData(nameof(SupplyPaidInFullCases))]
    public async Task CreateAsync_ShouldCloseSupply_WhenSupplyPaidInFull(
        decimal openingBalance, decimal due, decimal cash, decimal credit, decimal expectedBalance)
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var request = TransactionRequestFactory.Supply(partnerId, due, cash, credit);

        if (credit > 0)
        {
            await CreateAdvancePaymentAsync(partnerId, credit, PaymentDirection.Expense);
        }
        else if (openingBalance < 0)
        {
            await CreateOpenTransactionAsync(partnerId, 10_000, 5_000);
        }
        else if (openingBalance > 0)
        {
            await CreateAdvancePaymentAsync(partnerId, openingBalance);
        }

        // Act
        var createdTransaction = await PostTransactionAsync(request);

        // Assert
        var partner = await _context.PartnerBalances
            .FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(expectedBalance, partner.Total);

        var transaction = await _context.Transactions
            .FirstAsync(t => t.Id == createdTransaction.Id);
        Assert.Equal(TransactionStatus.Closed, transaction.Status);
    }

    public static readonly object[][] SupplyOverPayCases =
    [
        [ 0m, 10_000m, 15_000m, -5_000m ],
        [ 2_000m, 10_000m, 13_000m, -3_000m ], // debt will be paid separately below
        [ -2_000m, 10_000m, 15_000m, -7_000m ],
    ];

    [Theory, MemberData(nameof(SupplyOverPayCases))]
    public async Task CreateAsync_ShouldCreateAdvance_WhenSupplyOverPaid(
        decimal openingBalance, decimal due, decimal cash, decimal expectedAdvance)
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var debts = new List<CreateDebtPaymentRequest>();
        var openSupplies = new List<int>();

        if (openingBalance > 0)
        {
            var openSupplyId = await CreateOpenTransactionAsync(partnerId, 10_000m, 8_000m, TransactionType.Supply);
            debts.Add(new CreateDebtPaymentRequest(openSupplyId, 2_000m));
            openSupplies.Add(openSupplyId);
        }
        else if (openingBalance < 0)
        {
            await CreateAdvancePaymentAsync(partnerId, Math.Abs(openingBalance), PaymentDirection.Expense);
        }

        var request = TransactionRequestFactory.Supply(partnerId, due, cash, creditLocal: 0m, debts);

        // Act
        var createdTransaction = await PostTransactionAsync(request);

        // Assert
        var partner = await _context.PartnerBalances
            .FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(expectedAdvance, partner.Total);

        var payment = await _context.Payments
            .Include(x => x.Allocations)
            .Where(x => x.Allocations.Any(a => a.TransactionId == createdTransaction.Id))
            .SingleAsync();
        var advance = payment.Allocations.Single(x => x.TransactionId == null);
        Assert.Equal(cash - due, advance.Amount);

        var transaction = await _context.Transactions
            .FirstAsync(x => x.Id == createdTransaction.Id);
        Assert.Equal(TransactionStatus.Closed, transaction.Status);

        var suppliesToBeClosed = await _context.Transactions
            .Where(x => openSupplies.Contains(x.Id))
            .ToArrayAsync();

        Assert.All(suppliesToBeClosed, supply => Assert.True(supply.Status == TransactionStatus.Closed));
    }

    public static readonly object[][] SupplyUnderPayData =
    [
        // openingBalance , due , cash , creditUsed , expectedDebt
        [ 2_000m, 10_000m, 5_000m, 0, 7_000m],
        [  0m, 10_000m, 7_500m , 0m, 2_500m ],
        [ -2_000m, 10_000m, 6_000m, 2_000m, 2_000m ]
    ];

    [Theory, MemberData(nameof(SupplyUnderPayData))]
    public async Task CreateAsync_ShouldLeaveDebt_WhenSupplyUnderPaid(
        decimal openingBalance, decimal due, decimal cash, decimal creditUsed, decimal expectedDebt)
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var request = TransactionRequestFactory.Supply(partnerId, due, cash, creditUsed);

        if (openingBalance > 0)
        {
            await CreateOpenTransactionAsync(partnerId, 10_000, 8_000, TransactionType.Supply);
        }
        else if (openingBalance < 0)
        {
            await CreateAdvancePaymentAsync(partnerId, Math.Abs(openingBalance), PaymentDirection.Expense);
        }

        // Act
        var createdTransaction = await PostTransactionAsync(request);

        // Assert
        var partner = await _context.PartnerBalances
            .FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(expectedDebt, partner.Total);

        var transaction = await _context.Transactions
            .FirstAsync(t => t.Id == createdTransaction.Id);
        Assert.Equal(TransactionStatus.PartiallyPaid, transaction.Status);
        Assert.Equal(due - (cash + creditUsed), transaction.UnpaidAmount); // we owe partner
    }
}
