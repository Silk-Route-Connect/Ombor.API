using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a commercial transaction: Sale, Supply, or their refund counterparts.
/// One row per transaction header; detailed products are in <see cref="TransactionLine"/>.
/// </summary>
public class TransactionRecord : EntityBase
{
    /// <summary>
    /// Free-form notes visible to end-users; not used in calculations.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Human-readable sequential number (e.g., SA-2025-00013).
    /// </summary>
    public string? TransactionNumber { get; set; }

    /// <summary>
    /// Total due in local currency (sum of <see cref="TransactionLine.LineTotal"/>).
    /// </summary>
    public decimal TotalDue { get; set; }

    /// <summary>
    /// Total paid in local currency.
    /// </summary>
    public decimal TotalPaid { get; set; }

    /// <summary>
    /// Total amount to be paid.
    /// </summary>
    public decimal UnpaidAmount => TotalDue - TotalPaid;

    /// <summary>
    /// Transaction date-time in UTC.
    /// Local time can be derived via user's time-zone settings if needed.
    /// </summary>
    public DateTimeOffset DateUtc { get; set; }

    /// <summary>
    /// Commercial nature of the transaction (Sale, Supply, Refund etc.).
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// The status of the transaction.
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Foreign-key to the counterpart <see cref="Partner"/>.
    /// </summary>
    public int PartnerId { get; set; }

    /// <summary>
    /// Navigation property to the counterpart (customer / supplier).
    /// </summary>
    public required virtual Partner Partner { get; set; }

    /// <summary>
    /// If this transaction is a refund, points to the original transaction being refunded.
    /// <c>null</c> for normal Sale/Supply transactions.
    /// </summary>
    public int? RefundedTransactionId { get; set; }

    /// <summary>
    /// Navigation property to the original transactions
    /// (only when <see cref="Type"/> is a <see cref="TransactionType.SaleRefund"/> or <see cref="TransactionType.SupplyRefund"/>).
    /// </summary>
    public virtual TransactionRecord? RefundedTransaction { get; set; }

    /// <summary>
    /// Collection of refund transactions that reference this record.
    /// </summary>
    public virtual ICollection<TransactionRecord> Refunds { get; set; }

    /// <summary>
    /// Collection of detailed product/service lines belonging to this record.
    /// </summary>
    public virtual ICollection<TransactionLine> Lines { get; set; }

    /// <summary>
    /// Constructor initialises collections to avoid null reference checks.
    /// </summary>
    public TransactionRecord()
    {
        Refunds = new List<TransactionRecord>();
        Lines = new List<TransactionLine>();
    }
}
