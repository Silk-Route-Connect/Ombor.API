using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// The source side of a payment (rule 9): where the money came from. A <see cref="PaymentSourceType.Wallet"/>
/// component carries the <see cref="WalletId"/> and the amount moved through that wallet (net of change);
/// an <see cref="PaymentSourceType.Advance"/> component draws against the partner's advance claim and has no wallet.
/// </summary>
public class PaymentComponent : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    public required decimal Amount { get; set; }

    /// <summary>Where this component's money comes from (rule 9).</summary>
    public PaymentSourceType SourceType { get; set; } = PaymentSourceType.Wallet;

    /// <summary>The wallet the money moved through; null for an Advance source.</summary>
    public int? WalletId { get; set; }
    public virtual Wallet? Wallet { get; set; }

    public int PaymentId { get; set; }
    public virtual required Payment Payment { get; set; }
}
