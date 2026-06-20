using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Keyless projection (SQL view <c>View_PartnerBalance</c>) of a partner's balance, organization-scoped
/// so the global query filter isolates it like every other organization-owned entity.
/// </summary>
public class PartnerBalance : IOrganizationScoped
{
    public int OrganizationId { get; set; }
    public int PartnerId { get; init; }

    /// <summary>The partner's immutable opening balance (signed: positive = the partner owes us).</summary>
    public decimal OpeningBalance { get; init; }

    /// <summary>The partner's prepaid claim on us (we owe it back).</summary>
    public decimal PartnerAdvance { get; init; }

    /// <summary>Our prepaid claim on the partner (they owe it back).</summary>
    public decimal CompanyAdvance { get; init; }

    /// <summary>Unpaid Supply and SaleRefund totals: money we owe the partner.</summary>
    public decimal PayableDebt { get; init; }

    /// <summary>Unpaid Sale and SupplyRefund totals: money the partner owes us.</summary>
    public decimal ReceivableDebt { get; init; }

    /// <summary>Net balance (rule 12, sign per complexity notes §G): positive means the partner owes us.</summary>
    public decimal Total => OpeningBalance + (ReceivableDebt + CompanyAdvance) - (PayableDebt + PartnerAdvance);
}
