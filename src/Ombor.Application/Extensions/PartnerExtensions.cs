using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class PartnerExtensions
{
    public static void ApplyPayment(this Partner partner, Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (payment.Direction == PaymentDirection.Income)
        {
            partner.Balance += payment.AmountLocal;
        }
        else
        {
            partner.Balance -= payment.AmountLocal;
        }
    }

    public static bool CanHandle(this PartnerType partnerType, Contracts.Enums.TransactionType transactionType) => transactionType switch
    {
        Contracts.Enums.TransactionType.Sale => partnerType is PartnerType.Customer or PartnerType.All,
        Contracts.Enums.TransactionType.Supply => partnerType is PartnerType.Supplier or PartnerType.All,
        Contracts.Enums.TransactionType.SaleRefund => partnerType is PartnerType.Customer or PartnerType.All,
        Contracts.Enums.TransactionType.SupplyRefund => partnerType is PartnerType.Supplier or PartnerType.All,
        _ => false
    };
}
