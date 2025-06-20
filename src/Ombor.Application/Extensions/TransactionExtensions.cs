using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class TransactionExtensions
{
    public static PaymentAllocationType ToAllocationType(this TransactionType t) =>
    t switch
    {
        TransactionType.Sale => PaymentAllocationType.Sale,
        TransactionType.Supply => PaymentAllocationType.Supply,
        TransactionType.SaleRefund => PaymentAllocationType.SaleRefund,
        TransactionType.SupplyRefund => PaymentAllocationType.SupplyRefund,
        _ => throw new ArgumentOutOfRangeException(nameof(t))
    };
}
