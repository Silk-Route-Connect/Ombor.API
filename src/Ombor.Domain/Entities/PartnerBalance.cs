namespace Ombor.Domain.Entities;

public class PartnerBalance
{
    public int PartnerId { get; init; }
    public decimal PartnerAdvance { get; init; }
    public decimal CompanyAdvance { get; init; }
    public decimal PayableDebt { get; init; }
    public decimal ReceivableDebt { get; init; }
    public decimal Total => (PartnerAdvance + ReceivableDebt) - (CompanyAdvance + PayableDebt);
}
