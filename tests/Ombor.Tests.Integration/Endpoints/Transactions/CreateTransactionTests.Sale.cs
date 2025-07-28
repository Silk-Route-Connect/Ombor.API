using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Requests.Payment;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Factories;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public partial class CreateTransactionTests
{
    public static readonly object[][] SaleWithEqualPaymentCases =
    [
        [ 0m, 10_000m , 10_000m , 0m, 0m ],
        [ -5_000m, 10_000m, 10_000m, 0m, -5_000m ],
        [ 5_000m, 10_000m, 10_000m, 0, 5_000m ],
        [ 5_000m, 10_000m, 5_000m, 5_000m, 0m ] // advance consumed
    ];

    [Theory, MemberData(nameof(SaleWithEqualPaymentCases))]
    public async Task CreateAsync_ShouldCloseSale_WhenSalePaidInFull(
        decimal openingBalance, decimal due, decimal cash, decimal credit, decimal expectedBalance)
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();

        if (openingBalance < 0)
        {
            await CreateOpenTransactionAsync(partnerId, 10_000 + Math.Abs(openingBalance), 10_000, TransactionType.Sale);
        }
        else if (openingBalance > 0)
        {
            await CreateAdvancePaymentAsync(partnerId, openingBalance);
        }

        var request = TransactionRequestFactory.Sale(partnerId, due, cash, credit);

        // Act
        var createdTransaction = await PostTransactionAsync(request);

        // Assert
        var balance = await _context.PartnerBalances.FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(expectedBalance, balance.Total);

        var transaction = await _context.Transactions
            .Include(x => x.PaymentAllocations)
            .SingleAsync(t => t.Id == createdTransaction.Id);
        Assert.Equal(TransactionStatus.Closed, transaction.Status);
        Assert.Single(transaction.PaymentAllocations);
    }

    public static readonly object[][] SaleWithOverpaymentCases =
    [
        [  0m, 10_000m, 15_000m, 5_000m ],
        [ -2_000m, 10_000m, 13_000m, 3_000m ], // debt will be paid separately below
        [  2_000m, 10_000m, 15_000m, 7_000m ] // existing credit grows
    ];

    [Theory, MemberData(nameof(SaleWithOverpaymentCases))]
    public async Task CreateAsync_ShouldCreateAdvance_WhenSaleOverPaid(
        decimal openingBalance, decimal due, decimal cash, decimal expectedAdvance)
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var openSales = new List<int>();
        var debts = new List<CreateDebtPaymentRequest>();

        if (openingBalance < 0)
        {
            var openSaleId = await CreateOpenTransactionAsync(partnerId, 10_000m, 8_000m);
            debts.Add(new CreateDebtPaymentRequest(openSaleId, 2_000m));
            openSales.Add(openSaleId);
        }
        if (openingBalance > 0)
        {
            await CreateAdvancePaymentAsync(partnerId, openingBalance);
        }

        var request = TransactionRequestFactory.Sale(partnerId, due, cash, creditLocal: 0m, debts, refundChange: false);

        // Act
        var createdTransaction = await PostTransactionAsync(request);

        // Assert
        var balance = await _context.PartnerBalances
            .FirstAsync(x => x.PartnerId == partnerId);
        Assert.Equal(expectedAdvance, balance.Total);

        var payment = await _context.Payments
            .Include(x => x.Allocations)
            .Where(x => x.Allocations.Any(a => a.TransactionId == createdTransaction.Id))
            .FirstAsync();
        var advance = payment.Allocations.First(x => x.Type == PaymentAllocationType.AdvancePayment);
        Assert.Equal(cash - due, advance.Amount);

        var transaction = await _context.Transactions
            .FirstAsync(x => x.Id == createdTransaction.Id);
        Assert.Equal(TransactionStatus.Closed, transaction.Status);

        var salesToBeClosed = await _context.Transactions
            .Where(x => openSales.Contains(x.Id))
            .ToArrayAsync();

        Assert.All(salesToBeClosed, sale => Assert.True(sale.Status == TransactionStatus.Closed));
    }

    public static readonly object[][] SaleWithUnderpaymentCases =
    [
        [ -2_000m, 10_000m, 5_000m, 0m, -7_000m ],
        [ 0m, 10_000m,  7_500m, 0m, -2_500m ],
        [ 2_000m, 10_000m, 6_000m, 2_000m, -2_000m ]
    ];

    [Theory, MemberData(nameof(SaleWithUnderpaymentCases))]
    public async Task CreateAsync_ShouldLeaveDebt_WhenSaleUnderPaid(
        decimal openingBalance, decimal due, decimal cash, decimal creditUsed, decimal expectedDebt)
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var request = TransactionRequestFactory.Sale(partnerId, due, cash, creditUsed);

        if (openingBalance < 0)
        {
            await CreateOpenTransactionAsync(partnerId, 10_000, 8_000);
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
        Assert.Equal(expectedDebt, partner.Total);

        var transaction = await _context.Transactions
            .FirstAsync(t => t.Id == createdTransaction.Id);
        Assert.Equal(TransactionStatus.PartiallyPaid, transaction.Status);
        Assert.Equal(due - (cash + creditUsed), transaction.UnpaidAmount);
    }
}
