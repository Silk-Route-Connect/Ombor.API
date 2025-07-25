namespace Ombor.Contracts.Responses.Partner;

public sealed record PartnerBalanceDto(
    decimal Total,
    decimal PartnerAdvance,
    decimal CompanyAdvance,
    decimal PayableDebt,
    decimal ReceivableDebt);
