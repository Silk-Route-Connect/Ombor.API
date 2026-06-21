using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Tests.Common.Factories;

/// <summary>
/// Builds <see cref="CreateTransactionRequest"/> instances for the source/allocation payment model:
/// a single wallet source (<paramref name="paidAmount"/>) settles the transaction first, then any
/// <paramref name="settlements"/>; the remainder is handled per <paramref name="overpayment"/>.
/// </summary>
public static class TransactionRequestFactory
{
    /// <summary>Builds a Sale transaction request (single line: <c>unitPrice = due</c>, quantity 1).</summary>
    public static CreateTransactionRequest Sale(
        int partnerId,
        int productId,
        int warehouseId,
        decimal due,
        int? walletId,
        decimal paidAmount,
        OverpaymentHandling overpayment = OverpaymentHandling.Change,
        SettlementInput[]? settlements = null,
        DateOnly? dueDate = null)
        => Build(partnerId, TransactionType.Sale, productId, warehouseId, due, walletId, paidAmount, overpayment, settlements, dueDate);

    /// <summary>Builds a Supply transaction request (single line: <c>unitPrice = due</c>, quantity 1).</summary>
    public static CreateTransactionRequest Supply(
        int partnerId,
        int productId,
        int warehouseId,
        decimal due,
        int? walletId,
        decimal paidAmount,
        OverpaymentHandling overpayment = OverpaymentHandling.Change,
        SettlementInput[]? settlements = null,
        DateOnly? dueDate = null)
        => Build(partnerId, TransactionType.Supply, productId, warehouseId, due, walletId, paidAmount, overpayment, settlements, dueDate);

    private static CreateTransactionRequest Build(
        int partnerId,
        TransactionType type,
        int productId,
        int warehouseId,
        decimal due,
        int? walletId,
        decimal paidAmount,
        OverpaymentHandling overpayment,
        SettlementInput[]? settlements,
        DateOnly? dueDate)
    {
        var lines = new[]
        {
            new CreateTransactionLine(
                ProductId: productId,
                UnitPrice: due,
                Discount: 0m,
                DiscountType: DiscountType.Percentage,
                Quantity: 1),
        };

        return new CreateTransactionRequest(
            PartnerId: partnerId,
            Type: type,
            Notes: null,
            Lines: lines,
            WalletId: walletId,
            PaidAmount: paidAmount,
            Settlements: settlements,
            Overpayment: overpayment,
            Attachments: null!,
            WarehouseId: warehouseId,
            DueDate: dueDate);
    }
}
