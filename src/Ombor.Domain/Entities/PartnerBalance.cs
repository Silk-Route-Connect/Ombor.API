using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Keyless projection (SQL view <c>View_PartnerBalance</c>) of a partner's balance.
/// Organization-scoped so the global query filter isolates it like every other
/// organization-owned entity; the view surfaces <see cref="OrganizationId"/> from its partner.
/// </summary>
public class PartnerBalance : IOrganizationScoped
{
    public int OrganizationId { get; set; }
    public int PartnerId { get; init; }
    public decimal PartnerAdvance { get; init; }
    public decimal CompanyAdvance { get; init; }
    public decimal PayableDebt { get; init; }
    public decimal ReceivableDebt { get; init; }
    public decimal Total => (PartnerAdvance + ReceivableDebt) - (CompanyAdvance + PayableDebt);
}
