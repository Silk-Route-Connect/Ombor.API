using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class PartnerExtensions
{
    // Problem with checking the balance of a partner. When the transaction is added,
    // it immediately updates the balance of a partner, so if there was a supply
    // an amount of 100, and the payment was 0, and partner's prior balance was -50
    // it would now become +50, 100 - 50 because every transaction is an income for the partner
    // but here we can't check the state of a partner, earlier if he had -50 and we could say
    // we can withdraw -50 for the 100 supply, now it's already +50 and we can't do that.
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
