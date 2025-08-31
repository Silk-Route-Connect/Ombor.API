using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Configurations;

namespace Ombor.TestDataGenerator.Generators;

public static class PaymentGenerator
{
    private const string Uzs = "UZS";
    private const string Usd = "USD";
    private const decimal UzsStep = 1_000m; // Minimal denomination in UZS
    private const decimal UsdRate = 13_000m; // 1 USD = 13,000 UZS
    private const decimal UsdStepBase = 5m * UsdRate; // $5 * 13,000 = 65,000 UZS per USD component step

    private static readonly Random Rng = new();
    private static readonly Faker faker = new();

    private static readonly Dictionary<TransactionType, (PaymentDirection direction, PaymentAllocationType allocType)> Rules =
        new()
        {
            { TransactionType.Sale,         (PaymentDirection.Income,  PaymentAllocationType.Sale) },
            { TransactionType.Supply,       (PaymentDirection.Expense, PaymentAllocationType.Supply) },
            { TransactionType.SaleRefund,   (PaymentDirection.Expense, PaymentAllocationType.SaleRefund) },
            { TransactionType.SupplyRefund, (PaymentDirection.Income,  PaymentAllocationType.SupplyRefund) },
        };

    public static IReadOnlyList<Payment> GeneratePayments(TransactionRecord transaction, PaymentSeedSettings options)
    {
        if (!Rules.TryGetValue(transaction.Type, out var rule))
        {
            return [];
        }

        var payments = new List<Payment>();
        var remainingUnpaid = transaction.UnpaidAmount;

        if (remainingUnpaid <= 0m)
        {
            return payments;
        }

        // Scenario selection using all three probabilities explicitly
        var (isExact, isOverpay) = PickScenario(options.ChanceExactPay, options.ChanceOverpay, options.ChancePartialUnpaid);

        // Installment count (normalized bounds)
        var (minInstallmentsCount, maxInstallmentsCount) = options.InstallmentCountRange;
        var minInstallments = Math.Max(1, minInstallmentsCount);
        var maxInstallments = Math.Max(1, Math.Min(maxInstallmentsCount, options.MaxInstallmentsPerTransaction));

        var installmentCount = NextInt(minInstallments, maxInstallments);

        // Start at transaction date; allow same-day multiple payments but often advance a bit
        var currentDate = transaction.DateUtc;

        for (var installmentIndex = 1; installmentIndex <= installmentCount; installmentIndex++)
        {
            var isLastInstallment = installmentIndex == installmentCount;

            // If fully covered earlier, no more payments unless intentional overpay on last
            if (remainingUnpaid <= 0m && !isLastInstallment)
            {
                break;
            }

            // Tiny rounding remainders considered zero on the last step
            if (isLastInstallment && remainingUnpaid < 0.01m)
            {
                remainingUnpaid = 0m;
            }

            // Base (UZS) payment amount for this installment
            var installmentBaseAmount = DecideInstallmentAmount(remainingUnpaid, isLastInstallment, isExact, isOverpay);

            // Enforce thousand-floor and min 1,000 UZS
            var paymentTotal = FloorToThousand(installmentBaseAmount);
            if (paymentTotal < UzsStep)
            {
                continue;
            }

            var payment = new Payment
            {
                Type = PaymentType.Transaction,
                Direction = rule.direction,
                DateUtc = currentDate,
                PartnerId = transaction.PartnerId,
                Notes = faker.Lorem.Sentences(2),
            };

            // Components (1–2), pre-pick currencies with USD chance
            var componentCount = NextInt(1, 2);

            var componentCurrencies = new List<string>(capacity: componentCount);
            for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
            {
                var useUsd = NextDecimal() < options.ChanceUsdComponent;
                componentCurrencies.Add(useUsd ? Usd : Uzs);
            }

            // If all USD and total is not multiple of 65,000 UZS, force one UZS so we can hit exact total
            if (componentCurrencies.All(code => code == Usd) && (paymentTotal % UsdStepBase != 0m))
            {
                componentCurrencies[0] = Uzs;
            }

            // If minimal sum (one step per component) exceeds total, fallback to single-component UZS
            var minimalFeasibleSum = componentCurrencies.Sum(code => code == Uzs ? UzsStep : UsdStepBase);
            if (minimalFeasibleSum > paymentTotal)
            {
                componentCurrencies.Clear();
                componentCurrencies.Add(Uzs);
            }

            // Split base amount by per-currency base steps so components have real-life denominations
            var baseSteps = componentCurrencies.Select(code => code == Uzs ? UzsStep : UsdStepBase).ToArray();
            var baseParts = SplitBaseBySteps(paymentTotal, baseSteps);

            // Build components from base parts
            for (int componentIndex = 0; componentIndex < baseParts.Length; componentIndex++)
            {
                var currency = componentCurrencies[componentIndex];
                var basePortion = baseParts[componentIndex];

                if (basePortion <= 0m)
                {
                    continue;
                }

                if (currency == Uzs)
                {
                    // UZS component: already multiple of 1,000
                    payment.Components.Add(new PaymentComponent
                    {
                        Amount = RoundMoney(basePortion), // amount in UZS
                        Currency = Uzs,
                        ExchangeRate = 1m,
                        Method = RollMethod(),
                        Payment = payment
                    });
                }
                else
                {
                    // USD component: base portion multiple of 65,000 → amount multiple of $5
                    var usdAmount = RoundMoney(basePortion / UsdRate);
                    payment.Components.Add(new PaymentComponent
                    {
                        Amount = usdAmount,               // amount in USD
                        Currency = Usd,
                        ExchangeRate = UsdRate,
                        Method = RollMethod(),
                        Payment = payment
                    });
                }
            }

            // Allocations: first to the transaction up to remaining; leftover as advance/change
            var allocatedToTransaction = Math.Min(paymentTotal, Math.Max(0m, remainingUnpaid));
            // Clamp so TotalPaid never overshoots due to rounding
            var maxAllocatable = Math.Max(0m, transaction.TotalDue - transaction.TotalPaid);
            allocatedToTransaction = RoundMoney(Math.Min(allocatedToTransaction, maxAllocatable));

            if (allocatedToTransaction > 0m)
            {
                payment.Allocations.Add(new PaymentAllocation
                {
                    Amount = allocatedToTransaction,
                    Type = rule.allocType,
                    Transaction = transaction,
                    Payment = payment
                });

                remainingUnpaid -= allocatedToTransaction;
                transaction.TotalPaid += allocatedToTransaction;
            }

            var leftover = RoundMoney(paymentTotal - allocatedToTransaction);
            if (leftover > 0m)
            {
                var sendAsChange = NextDecimal() < options.ChanceChangeReturnVsAdvance;
                var allocationType = sendAsChange ? PaymentAllocationType.ChangeReturn : PaymentAllocationType.AdvancePayment;

                payment.Allocations.Add(new PaymentAllocation
                {
                    Amount = leftover,
                    Type = allocationType,
                    Transaction = null,
                    Payment = payment
                });
            }

            payments.Add(payment);

            // Advance date for next installment (monotonic, bounded within MaxDaysAfterTransaction)
            currentDate = NextLaterDate(transaction.DateUtc, options.MaxDaysAfterTransaction, currentDate);
        }

        // Finalize transaction status
        transaction.Status = transaction.TotalPaid >= transaction.TotalDue
            ? TransactionStatus.Closed
            : TransactionStatus.Open;

        return payments;
    }

    /// <summary>
    /// Floors to thousands (e.g., 135123 -> 135000). Non-negative input expected.
    /// </summary>
    private static decimal FloorToThousand(decimal amount)
    {
        if (amount <= 0m)
        {
            return 0m;
        }

        return Math.Floor(amount / UzsStep) * UzsStep;
    }

    /// <summary>
    /// Split a base amount across components using per-component base steps,
    /// summing exactly to 'total'. Steps must be positive.
    /// </summary>
    private static decimal[] SplitBaseBySteps(decimal total, decimal[] steps)
    {
        if (total <= 0m)
        {
            return steps.Length == 1
                ? new[] { 0m }
                : Enumerable.Repeat(0m, steps.Length).ToArray();
        }

        if (steps.Length == 1)
        {
            return new[] { total };
        }

        // Random weights for proportional split
        var weights = new decimal[steps.Length];
        decimal weightsSum = 0m;
        for (int i = 0; i < steps.Length; i++)
        {
            weights[i] = 0.5m + NextDecimal(); // 0.5 .. 1.5
            weightsSum += weights[i];
        }

        // Tentative proportional allocations (in base)
        var tentative = new decimal[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            tentative[i] = total * (weights[i] / weightsSum);
        }

        // Floor to step and ensure at least one step where feasible
        var parts = new decimal[steps.Length];
        decimal allocated = 0m;
        for (int i = 0; i < steps.Length; i++)
        {
            var step = steps[i];
            var floored = Math.Floor(tentative[i] / step) * step;

            if (floored < step && total >= step)
            {
                // Give at least one step if possible without exceeding total
                floored = step;
            }

            // Cap if we exceed remaining due to rounding
            if (allocated + floored > total)
            {
                floored = Math.Floor((total - allocated) / step) * step;
            }

            parts[i] = floored;
            allocated += floored;
        }

        // Distribute remainder by adding full steps where possible
        var remainder = total - allocated; // multiple of 1,000
        var safetyCounter = 0;
        while (remainder > 0m && safetyCounter < 10_000)
        {
            var index = NextInt(0, steps.Length - 1);
            var step = steps[index];

            if (remainder >= step)
            {
                parts[index] += step;
                remainder -= step;
            }
            else
            {
                // Try any slot that fits
                var foundSlot = false;
                for (int j = 0; j < steps.Length; j++)
                {
                    if (remainder >= steps[j])
                    {
                        parts[j] += steps[j];
                        remainder -= steps[j];
                        foundSlot = true;
                        break;
                    }
                }

                if (!foundSlot)
                {
                    break; // remainder smaller than every step
                }
            }

            safetyCounter++;
        }

        // Final safety: if anything still left, dump to the smallest-step slot
        if (remainder > 0m)
        {
            var minStep = steps.Min();
            var minStepIndex = Array.IndexOf(steps, minStep);
            parts[minStepIndex] += remainder;
        }

        // Ensure exact sum
        var partsSum = parts.Sum();
        if (partsSum != total)
        {
            var diff = total - partsSum;
            var minStep = steps.Min();
            var minStepIndex = Array.IndexOf(steps, minStep);
            parts[minStepIndex] += diff;
        }

        return parts;
    }

    /// <summary>
    /// Returns a timestamp that is >= current and <= (transactionDate + maxDaysAfter).
    /// Adds a small random hours/minutes increment for realism.
    /// </summary>
    private static DateTimeOffset NextLaterDate(DateTimeOffset transactionDate, int maxDaysAfter, DateTimeOffset current)
    {
        var deltaDays = NextInt(0, Math.Max(1, Math.Min(5, maxDaysAfter))); // 0..5 days
        var candidate = current
            .AddDays(deltaDays)
            .AddHours(NextInt(0, 3))
            .AddMinutes(NextInt(0, 59));
        var max = transactionDate.AddDays(maxDaysAfter);
        return candidate <= max ? candidate : max;
    }

    private static PaymentMethod RollMethod()
    {
        var randomSample = NextDecimal();

        if (randomSample < 0.60m)
        {
            return PaymentMethod.Cash;
        }

        if (randomSample < 0.85m)
        {
            return PaymentMethod.Card;
        }

        return PaymentMethod.Bank;
    }

    private static (bool isExact, bool isOverpay) PickScenario(decimal probabilityExact, decimal probabilityOverpay, decimal probabilityPartial)
    {
        // Normalize (tolerate sums != 1); if all zero, default to partial
        var sum = probabilityExact + probabilityOverpay + probabilityPartial;

        if (sum <= 0m)
        {
            return (isExact: false, isOverpay: false); // partial
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

    private static decimal DecideInstallmentAmount(
        decimal remainingBase,
        bool isLast,
        bool isExactScenario,
        bool isOverpayScenario)
    {
        if (!isLast)
        {
            var slice = remainingBase * RandomBetween(0.20m, 0.60m);
            return RoundMoney(slice);
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

        // Partial unpaid: pay 20%..80% of remaining, but leave something
        var pay = remainingBase * RandomBetween(0.20m, 0.80m);
        var capped = Math.Min(RoundMoney(pay), Math.Max(0m, remainingBase - 1m));

        return Math.Max(0m, capped);
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static decimal NextDecimal() => (decimal)Rng.NextDouble();
    private static int NextInt(int minInclusive, int maxInclusive) => Rng.Next(minInclusive, maxInclusive + 1);
    private static decimal RandomBetween(decimal min, decimal max) => min + ((max - min) * NextDecimal());
}

