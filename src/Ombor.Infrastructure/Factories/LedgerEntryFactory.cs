using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Infrastructure.Factories;

internal static class LedgerEntryFactory
{
    public static LedgerEntry FromTransaction(TransactionRecord transaction) => new()
    {
        CreatedAtUtc = transaction.DateUtc,
        AmountLocal = GetAmount(transaction),
        Notes = null,
        Type = LedgerType.InvoiceCreated,
        Source = nameof(TransactionRecord),
        SourceId = transaction.Id,
        PartnerId = transaction.PartnerId,
        Partner = null! // will be set by EF
    };

    public static LedgerEntry FromPayment(Payment payment)
    {
        if (payment.PartnerId is null)
        {
            throw new InvalidOperationException("Cannot created ledger entry for payment without Partner.");
        }

        var entry = new LedgerEntry()
        {
            CreatedAtUtc = payment.DateUtc,
            AmountLocal = GetAmount(payment),
            Notes = null,
            Type = GetType(payment),
            Source = nameof(Payment),
            SourceId = payment.Id,
            PartnerId = payment.PartnerId.Value,
            Partner = null! // will be set by EF
        };

        return entry;
    }

    private static decimal GetAmount(TransactionRecord transaction) =>
        transaction.Type switch
        {
            TransactionType.Sale => -transaction.TotalDue,           // partner pays
            TransactionType.Supply => +transaction.TotalDue,         // company pays
            TransactionType.SaleRefund => +transaction.TotalDue,     // company pays out
            TransactionType.SupplyRefund => -transaction.TotalDue,   // company gets refund
            _ => 0m
        };

    private static decimal GetAmount(Payment payment) =>
        payment.Direction == PaymentDirection.Income
        ? +payment.AmountLocal  // partner pays
        : -payment.AmountLocal; // company pays

    private static LedgerType GetType(Payment payment) =>
        payment switch
        {
            { Type: PaymentType.Transaction, Method: PaymentMethod.AccountBalance } => LedgerType.InvoicePaidUsingBalance,
            _ => LedgerType.InvoicePaid,
        };
}
