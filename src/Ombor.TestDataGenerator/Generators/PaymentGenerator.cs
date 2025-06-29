using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.TestDataGenerator.Generators;

public static class PaymentGenerator
{
    private const string Locale = "en";

    /// <summary>
    /// Generates one-or-more payments for each transaction,
    /// splitting <see cref="TransactionRecord.TotalPaid"/> into 1–5 chunks.
    /// </summary>
    public static List<Payment> Generate(IEnumerable<TransactionRecord> transactions)
    {
        var faker = new Faker(Locale);
        var payments = new List<Payment>();

        foreach (var transaction in transactions)
        {
            var totalPaid = Math.Clamp(transaction.TotalPaid, 0, transaction.TotalDue);

            int chunks = faker.Random.Int(1, 5);

            if (chunks == 1)
            {
                var payment = CreatePayment(faker, transaction, transaction.DateUtc, totalPaid);
                var allocation = new PaymentAllocation
                {
                    AppliedAmount = totalPaid,
                    Type = Enum.Parse<PaymentAllocationType>(transaction.Type.ToString()),
                    Payment = payment,
                    Transaction = transaction,
                };
                payment.Allocations.Add(allocation);
                payments.Add(payment);

                continue;
            }

            var splits = SplitAmount(totalPaid, chunks, faker);
            foreach (var amount in splits)
            {
                // scatter dates ±3 days
                var payDate = faker.Date.BetweenOffset(
                    transaction.DateUtc.AddDays(-3),
                    transaction.DateUtc.AddDays(3));

                var payment = CreatePayment(faker, transaction, payDate, amount);
                payment.Allocations.Add(new PaymentAllocation
                {
                    AppliedAmount = amount,
                    Type = Enum.Parse<PaymentAllocationType>(transaction.Type.ToString()),
                    Payment = payment,
                    Transaction = transaction,
                });
                payments.Add(payment);
            }
        }

        return payments;
    }

    private static Payment CreatePayment(
        Faker faker,
        TransactionRecord txn,
        DateTimeOffset date,
        decimal amount)
    {
        var currency = faker.Random.Enum<PaymentCurrency>();
        return new Payment
        {
            PartnerId = txn.PartnerId,
            DateUtc = date,
            Method = (PaymentMethod)faker.Random.Number(1, 3),
            Currency = currency,
            Type = PaymentType.Transaction,
            Direction = GetDirection(txn.Type),
            ExchangeRate = 1m,
            Amount = amount,
            AmountLocal = amount * 1m,
            ExternalReference = faker.Finance.RoutingNumber(),
            Notes = faker.Lorem.Sentence()
        };
    }

    private static List<decimal> SplitAmount(
        decimal total,
        int count,
        Faker faker)
    {
        // generate (count-1) cut points in [0, total]
        var points = new List<decimal> { 0m, total }; // 0, 100, 50, 40, 60
        for (int i = 0; i < count - 1; i++)
        {
            points.Add(faker.Random.Decimal(0, total));
        }

        points.Sort(); // 0, 40, 50, 60, 100

        var chunks = new List<decimal>();
        for (int i = 1; i < points.Count; i++)
        {
            // difference between successive cut points
            var diff = points[i] - points[i - 1]; // 40 - 0, 50 - 40, 60 - 50, 100 - 60 -> 40 + 10 + 10 + 40
            chunks.Add(Math.Round(diff, 2));
        }

        return chunks;
    }

    private static PaymentDirection GetDirection(TransactionType transactionType)
        => transactionType switch
        {
            TransactionType.Sale or TransactionType.SupplyRefund => PaymentDirection.Income,
            TransactionType.Supply or TransactionType.SaleRefund => PaymentDirection.Expense,
            TransactionType.None => throw new InvalidOperationException($"Cannot get payment direction for transaction of type: {transactionType}"),
            _ => throw new ArgumentOutOfRangeException($"Could not resolve direction for transaction type: {transactionType}"),
        };
}