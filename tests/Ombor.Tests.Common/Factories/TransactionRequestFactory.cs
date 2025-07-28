using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Tests.Common.Factories;

public static class TransactionRequestFactory
{
    /// <summary>
    /// Builds a Sale transaction request.
    /// </summary>
    public static CreateTransactionRequest Sale(
        int partnerId,
        decimal totalDue,
        decimal cashLocal,
        decimal creditLocal = 0m,
        IList<CreateDebtPaymentRequest>? debts = null,
        bool refundChange = true)
        => BuildRequest(
            partnerId,
            TransactionType.Sale,
            totalDue,
            cashLocal,
            creditLocal,
            debts,
            refundChange);

    /// <summary>
    /// Builds a Supply transaction request.
    /// </summary>
    public static CreateTransactionRequest Supply(
        int partnerId,
        decimal totalDue,
        decimal cashLocal,
        decimal creditLocal = 0m,
        IList<CreateDebtPaymentRequest>? debts = null,
        bool refundChange = true)
        => BuildRequest(
            partnerId,
            TransactionType.Supply,
            totalDue,
            cashLocal,
            creditLocal,
            debts,
            refundChange);

    private static CreateTransactionRequest BuildRequest(
        int partnerId,
        TransactionType type,
        decimal totalDue,
        decimal cashLocal,
        decimal creditLocal,
        IList<CreateDebtPaymentRequest>? debts,
        bool refundChange)
    {
        var payments = new List<CreatePaymentRequest>();
        var debtAmount = debts?.Count > 0
            ? debts.Sum(x => x.Amount)
            : 0;

        if (cashLocal > 0)
        {
            payments.Add(new CreatePaymentRequest(cashLocal + debtAmount, 1m, "UZS", PaymentMethod.Cash));
        }

        if (creditLocal > 0)
        {
            payments.Add(new CreatePaymentRequest(creditLocal, 1m, "UZS", PaymentMethod.AccountBalance));
        }

        // single line that matches <totalDue>; tests don't vary products yet
        var lines = new[]
        {
            new CreateTransactionLine(
                ProductId : 1,
                UnitPrice : totalDue,
                Discount  : 0m,
                Quantity  : 1)
        };

        return new CreateTransactionRequest(
            PartnerId: partnerId,
            Type: type,
            Lines: lines,
            Notes: null,
            Payments: [.. payments],
            DebtPayments: debts?.ToArray(),
            ShouldReturnChange: refundChange,
            Attachments: null);
    }
}
