using Ombor.Domain.Exceptions;

namespace Ombor.Application.Extensions;

internal static class EnumExtensions
{
    public static Domain.Enums.PaymentMethod ParseToDomain(this Contracts.Enums.PaymentMethod value)
    {
        if (Enum.TryParse<Domain.Enums.PaymentMethod>(value.ToString(), ignoreCase: true, out var paymentMethod))
        {
            return paymentMethod;
        }

        throw new EnumParseException<Contracts.Enums.PaymentMethod, Domain.Enums.PaymentMethod>();
    }

    public static Domain.Enums.PaymentCurrency ParseToDomain(this Contracts.Enums.PaymentCurrency value)
    {
        if (Enum.TryParse<Domain.Enums.PaymentCurrency>(value.ToString(), ignoreCase: true, out var paymentCurrency))
        {
            return paymentCurrency;
        }

        throw new EnumParseException<Contracts.Enums.PaymentCurrency, Domain.Enums.PaymentCurrency>();
    }

    public static Domain.Enums.PaymentDirection GetPaymentDirection(this Contracts.Enums.TransactionType value)
    {
        if (value is Contracts.Enums.TransactionType.Sale or Contracts.Enums.TransactionType.SupplyRefund)
        {
            return Domain.Enums.PaymentDirection.Income;
        }

        return Domain.Enums.PaymentDirection.Expense;
    }

    public static Domain.Enums.PaymentAllocationType GetPaymentAllocationType(this Contracts.Enums.TransactionType value)
        => value switch
        {
            Contracts.Enums.TransactionType.Sale => Domain.Enums.PaymentAllocationType.Sale,
            Contracts.Enums.TransactionType.Supply => Domain.Enums.PaymentAllocationType.Supply,
            Contracts.Enums.TransactionType.SaleRefund => Domain.Enums.PaymentAllocationType.SaleRefund,
            Contracts.Enums.TransactionType.SupplyRefund => Domain.Enums.PaymentAllocationType.SupplyRefund,
            _ => throw new EnumParseException<Contracts.Enums.TransactionType, Domain.Enums.PaymentAllocationType>(),
        };
}
