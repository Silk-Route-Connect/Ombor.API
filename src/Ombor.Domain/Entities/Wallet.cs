using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// A money location (Cash / Card / Bank). Master data, archivable. Its balance is never
/// stored — it is computed per read from the opening balance, wallet-sourced payment
/// components, and inter-wallet transfers (rules 12, 15, 16).
/// </summary>
public class Wallet : EntityBase, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>Display name, unique within the organization.</summary>
    public required string Name { get; set; }

    /// <summary>The money location this wallet represents. Immutable after creation (rule 16).</summary>
    public WalletType Type { get; set; }

    /// <summary>The balance recorded when the wallet was created. Immutable auditable event (rule 16).</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>Whether the wallet is archived (hidden from default lists, still counts in totals — rule 31).</summary>
    public bool IsArchived { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    /// <summary>Transfers sent from this wallet.</summary>
    public virtual ICollection<WalletTransfer> OutgoingTransfers { get; set; } = [];

    /// <summary>Transfers received into this wallet.</summary>
    public virtual ICollection<WalletTransfer> IncomingTransfers { get; set; } = [];

    /// <summary>Payment components sourced from this wallet — the payment side of the computed balance (rule 15).</summary>
    public virtual ICollection<PaymentComponent> Components { get; set; } = [];
}
