using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Configurations;

namespace Ombor.TestDataGenerator.Generators;

public static class PaymentGenerator
{
    private const decimal UZS_STEP = 1_000m; // Minimal denomination in UZS

    private static readonly Random Rng = new();
    private static readonly Faker faker = new();

    // A transaction payment's direction follows the transaction type (rules 8–10).
    private static readonly Dictionary<TransactionType, PaymentDirection> Directions = new()
    {
        { TransactionType.Sale,         PaymentDirection.Income },
        { TransactionType.Supply,       PaymentDirection.Expense },
        { TransactionType.SaleRefund,   PaymentDirection.Expense },
        { TransactionType.SupplyRefund, PaymentDirection.Income },
    };

    /// <summary>
    /// Generates source/allocation payments for a transaction (rules 8–10, 15): each payment draws from
    /// <paramref name="walletId"/> (one Wallet component, UZS) and settles the transaction; any overpay on the
    /// final installment becomes an AdvanceCredit or a ChangeReturn (the latter net out of the wallet component).
    /// </summary>
    public static IReadOnlyList<Payment> GeneratePayments(TransactionRecord transaction, int walletId, PaymentSeedSettings options)
    {
        if (!Directions.TryGetValue(transaction.Type, out var direction))
        {
            return [];
        }

        var payments = new List<Payment>();
        var remainingUnpaid = transaction.UnpaidAmount;

        if (remainingUnpaid <= 0m)
        {
            return payments;
        }

        var (isExact, isOverpay) = PickScenario(options.ChanceExactPay, options.ChanceOverpay, options.ChancePartialUnpaid);

        var (minInstallmentsCount, maxInstallmentsCount) = options.InstallmentCountRange;
        var minInstallments = Math.Max(1, minInstallmentsCount);
        var maxInstallments = Math.Max(1, Math.Min(maxInstallmentsCount, options.MaxInstallmentsPerTransaction));
        var installmentCount = NextInt(minInstallments, maxInstallments);

        var currentDate = transaction.DateUtc;

        for (var installmentIndex = 1; installmentIndex <= installmentCount; installmentIndex++)
        {
            var isLastInstallment = installmentIndex == installmentCount;

            if (remainingUnpaid <= 0m && !isLastInstallment)
            {
                break;
            }

            if (isLastInstallment && remainingUnpaid < 0.01m)
            {
                remainingUnpaid = 0m;
            }

            var installmentBaseAmount = DecideInstallmentAmount(remainingUnpaid, isLastInstallment, isExact, isOverpay);
            var paymentTotal = FloorToThousand(installmentBaseAmount);
            if (paymentTotal < UZS_STEP)
            {
                continue;
            }

            var maxAllocatable = Math.Max(0m, transaction.TotalDue - transaction.TotalPaid);
            var allocatedToTransaction = RoundMoney(Math.Min(paymentTotal, maxAllocatable));
            var leftover = RoundMoney(paymentTotal - allocatedToTransaction);

            // Overpay on a settled transaction is parked as an advance, or handed straight back as change (rule 15).
            var sendAsChange = NextDecimal() < options.ChanceChangeReturnVsAdvance;
            var parkAdvance = leftover > 0m && !sendAsChange;

            // Rule 9 source: the whole amount when parking an advance, net of change when returning it.
            var sourceAmount = allocatedToTransaction + (parkAdvance ? leftover : 0m);
            if (sourceAmount <= 0m)
            {
                continue;
            }

            var payment = new Payment
            {
                Type = PaymentType.Transaction,
                Direction = direction,
                DateUtc = currentDate,
                PartnerId = transaction.PartnerId,
                WalletId = walletId,
                Notes = faker.Lorem.Sentence(),
            };

            payment.Components.Add(new PaymentComponent
            {
                Payment = payment,
                SourceType = PaymentSourceType.Wallet,
                WalletId = walletId,
                Amount = sourceAmount,
            });

            if (allocatedToTransaction > 0m)
            {
                payment.Allocations.Add(new PaymentAllocation
                {
                    Payment = payment,
                    Transaction = transaction,
                    Amount = allocatedToTransaction,
                    Type = PaymentAllocationType.TransactionSettlement,
                });

                remainingUnpaid -= allocatedToTransaction;
                transaction.TotalPaid += allocatedToTransaction;
            }

            if (leftover > 0m)
            {
                payment.Allocations.Add(new PaymentAllocation
                {
                    Payment = payment,
                    Transaction = null,
                    Amount = leftover,
                    Type = parkAdvance ? PaymentAllocationType.AdvanceCredit : PaymentAllocationType.ChangeReturn,
                });
            }

            payments.Add(payment);
            currentDate = NextLaterDate(transaction.DateUtc, options.MaxDaysAfterTransaction, currentDate);
        }

        transaction.Status = transaction.TotalPaid >= transaction.TotalDue
            ? TransactionStatus.Closed
            : TransactionStatus.Open;

        return payments;
    }

    private static decimal FloorToThousand(decimal amount)
        => amount <= 0m ? 0m : Math.Floor(amount / UZS_STEP) * UZS_STEP;

    private static DateTimeOffset NextLaterDate(DateTimeOffset transactionDate, int maxDaysAfter, DateTimeOffset current)
    {
        var deltaDays = NextInt(0, Math.Max(1, Math.Min(5, maxDaysAfter)));
        var candidate = current
            .AddDays(deltaDays)
            .AddHours(NextInt(0, 3))
            .AddMinutes(NextInt(0, 59));
        var max = transactionDate.AddDays(maxDaysAfter);
        return candidate <= max ? candidate : max;
    }

    private static (bool isExact, bool isOverpay) PickScenario(decimal probabilityExact, decimal probabilityOverpay, decimal probabilityPartial)
    {
        var sum = probabilityExact + probabilityOverpay + probabilityPartial;
        if (sum <= 0m)
        {
            return (false, false); // partial
        }

        var normalizedExact = probabilityExact / sum;
        var normalizedOverpay = probabilityOverpay / sum;
        var randomSample = NextDecimal();

        if (randomSample < normalizedExact)
        {
            return (true, false);
        }

        if (randomSample < normalizedExact + normalizedOverpay)
        {
            return (false, true);
        }

        return (false, false); // partial
    }

    private static decimal DecideInstallmentAmount(decimal remainingBase, bool isLast, bool isExactScenario, bool isOverpayScenario)
    {
        if (!isLast)
        {
            return RoundMoney(remainingBase * RandomBetween(0.20m, 0.60m));
        }

        if (isExactScenario)
        {
            return RoundMoney(Math.Max(0m, remainingBase));
        }

        if (isOverpayScenario)
        {
            var bump = Math.Max(remainingBase, 1m) * RandomBetween(0.01m, 0.15m);
            return RoundMoney(Math.Max(0m, remainingBase) + bump);
        }

        // Partial unpaid: pay 20%..80% of remaining, but leave something.
        var pay = remainingBase * RandomBetween(0.20m, 0.80m);
        var capped = Math.Min(RoundMoney(pay), Math.Max(0m, remainingBase - 1m));

        return Math.Max(0m, capped);
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static decimal NextDecimal() => (decimal)Rng.NextDouble();
    private static int NextInt(int minInclusive, int maxInclusive) => Rng.Next(minInclusive, maxInclusive + 1);
    private static decimal RandomBetween(decimal min, decimal max) => min + ((max - min) * NextDecimal());
}
