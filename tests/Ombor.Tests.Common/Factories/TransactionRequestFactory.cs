using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transactions;

namespace Ombor.Tests.Common.Factories;

public static class TransactionRequestFactory
{
    public static CreateTransactionRequest GenerateValidCreateRequest(int partnerId, TransactionType type, decimal totalPaid, decimal totalDue)
        => new(
            PartnerId: partnerId,
            Type: type,
            Notes: $"Test {type} Transaction",
            TotalPaid: totalPaid,
            ExchangeRate: 1,
            Currency: PaymentCurrency.UZS,
            PaymentMethod: PaymentMethod.Cash,
            Attachments: null,
            Lines: GenerateLines(totalDue));

    private static CreateTransactionLine[] GenerateLines(decimal totalAmount)
    {
        var lineTotalPrice = totalAmount / 2;
        var productPrice = lineTotalPrice / 2;
        var line1 = new CreateTransactionLine(1, productPrice, 2, 0);
        var line2 = new CreateTransactionLine(2, productPrice, 2, 0);

        return [line1, line2];
    }
}
