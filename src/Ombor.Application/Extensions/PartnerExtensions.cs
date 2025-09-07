using Ombor.Contracts.Enums;
using Ombor.Domain.Entities;

namespace Ombor.Application.Extensions;

internal static class PartnerExtensions
{
    public static bool CanHandleTransaction(this Partner partner, TransactionType type)
    {
        if (partner.Type == Domain.Enums.PartnerType.Customer)
        {
            return type == TransactionType.Sale || type == TransactionType.SaleRefund;
        }

        if (partner.Type == Domain.Enums.PartnerType.Supplier)
        {
            return type == TransactionType.Supply || type == TransactionType.SupplyRefund;
        }

        return true;
    }
}
