namespace Ombor.Application.Extensions;

internal static class EnumExtensions
{
    public static Domain.Enums.TransactionStatus ToDomainStatus(this Contracts.Enums.TransactionStatus status)
    {
        if (Enum.TryParse<Domain.Enums.TransactionStatus>(status.ToString(), out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast between domain transaction status and contract transaction status: {status}");
    }

    public static Domain.Enums.TransactionType ToDomainType(this Contracts.Enums.TransactionType type)
    {
        if (Enum.TryParse<Domain.Enums.TransactionType>(type.ToString(), out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast between domain transaction type and contract transaction type: {type}");
    }

    public static Domain.Enums.PaymentDirection GetPaymentDirection(this Contracts.Enums.TransactionType type)
        => type switch
        {
            Contracts.Enums.TransactionType.Sale or
            Contracts.Enums.TransactionType.SupplyRefund => Domain.Enums.PaymentDirection.Income,
            Contracts.Enums.TransactionType.Supply or
            Contracts.Enums.TransactionType.SaleRefund => Domain.Enums.PaymentDirection.Expense,
            _ => throw new InvalidCastException($"Could not map transaction type ({type}) to payment direction.")
        };

    public static Domain.Enums.PaymentMethod ToDomainPaymentMethod(this Contracts.Enums.PaymentMethod method)
    {
        if (Enum.TryParse<Domain.Enums.PaymentMethod>(method.ToString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast between domain payment method and contract payment method: {method}.");
    }

    public static Domain.Enums.PaymentAllocationType ToPaymentAllocationType(this Domain.Enums.TransactionType type)
    {
        if (Enum.TryParse<Domain.Enums.PaymentAllocationType>(type.ToString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast between payment allocation type and transaction type: {type}.");
    }
}
