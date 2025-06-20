using Ombor.Domain.Enums;

namespace Ombor.Application.Extensions;

internal static class PartnerTypeExtensions
{
    public static bool CanHandle(this PartnerType partnerType, Contracts.Enums.TransactionType txnType) => txnType switch
    {
        Contracts.Enums.TransactionType.Sale => partnerType is PartnerType.Customer or PartnerType.All,
        Contracts.Enums.TransactionType.Supply => partnerType is PartnerType.Supplier or PartnerType.All,
        Contracts.Enums.TransactionType.SaleRefund => partnerType is PartnerType.Customer or PartnerType.All,
        Contracts.Enums.TransactionType.SupplyRefund => partnerType is PartnerType.Supplier or PartnerType.All,
        _ => false
    };
}
