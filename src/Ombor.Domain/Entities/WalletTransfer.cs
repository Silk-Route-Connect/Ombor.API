using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// An immutable inter-wallet money movement (rule 16). Moves <see cref="Amount"/> out of
/// <see cref="FromWallet"/> and into <see cref="ToWallet"/> atomically; corrections are
/// counter-transfers, never edits or deletes.
/// </summary>
public class WalletTransfer : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset DateUtc { get; set; }
    public string? CreatedBy { get; set; }

    public int FromWalletId { get; set; }
    public virtual required Wallet FromWallet { get; set; }

    public int ToWalletId { get; set; }
    public virtual required Wallet ToWallet { get; set; }
}
