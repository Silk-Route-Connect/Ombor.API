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

    public static Domain.Enums.PaymentDirection GetPaymentDirection(this Domain.Enums.TransactionType type)
        => type switch
        {
            Domain.Enums.TransactionType.Sale or
            Domain.Enums.TransactionType.SupplyRefund => Domain.Enums.PaymentDirection.Income,
            Domain.Enums.TransactionType.Supply or
            Domain.Enums.TransactionType.SaleRefund => Domain.Enums.PaymentDirection.Expense,
            _ => throw new InvalidCastException($"Could not map transaction type ({type}) to payment direction.")
        };

    public static Domain.Enums.DiscountType ToDomainDiscountType(this Contracts.Enums.DiscountType type)
    {
        if (Enum.TryParse<Domain.Enums.DiscountType>(type.ToString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast between domain discount type and contract discount type: {type}.");
    }

    public static Domain.Enums.PaymentType ToDomainType(this Contracts.Enums.PaymentType type)
    {
        if (Enum.TryParse<Domain.Enums.PaymentType>(type.ToString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast between domain payment type and contract payment type: {type}.");
    }

    public static Domain.Enums.PaymentDirection ToDomainDirection(this Contracts.Enums.PaymentDirection direction)
    {
        if (Enum.TryParse<Domain.Enums.PaymentDirection>(direction.ToString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast between domain payment direction and contract payment direction: {direction}.");
    }

    public static Domain.Enums.WalletType ToDomainType(this Contracts.Enums.WalletType type)
    {
        if (Enum.TryParse<Domain.Enums.WalletType>(type.ToString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast between domain wallet type and contract wallet type: {type}.");
    }

    public static Domain.Enums.OrderStatus ToDomainStatus(this Contracts.Enums.OrderStatus status)
    {
        if (Enum.TryParse<Domain.Enums.OrderStatus>(status.ToString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast contract order status: '{status}' to domain order status.");
    }

    public static Domain.Enums.StockAdjustmentDirection ToDomainDirection(this Contracts.Enums.StockAdjustmentDirection direction)
    {
        if (Enum.TryParse<Domain.Enums.StockAdjustmentDirection>(direction.ToString(), ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidCastException($"Could not cast contract stock-adjustment direction: '{direction}' to domain.");
    }
}
