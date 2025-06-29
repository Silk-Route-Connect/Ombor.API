using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class PartnerExtensions
{
    public static bool HasAvailableBalance(this Partner partner, TransactionRecord transaction)
    {
        ArgumentNullException.ThrowIfNull(partner);
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.Type is TransactionType.Supply && partner.Balance < 0)
        {
            return true;
        }

        if (transaction.Type is TransactionType.Sale && partner.Balance > 0)
        {
            return true;
        }

        return false;
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
