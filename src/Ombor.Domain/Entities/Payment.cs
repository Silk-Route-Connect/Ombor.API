using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Money movement (cash in/out) recorded in the system – could settle a transaction,
/// top-up a partner's balance, pay payroll, or record a general expense / income.
/// </summary>
public class Payment : EntityBase
{
    /// <summary>
    /// Optional free-form note visible to end-users.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Reference from an external system (bank statement ID, POS slip, etc.).
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Amount in the original currency.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Amount converted to local currency at <see cref="ExchangeRate"/>.
    /// </summary>
    public decimal AmountLocal { get; set; }

    /// <summary>
    /// Exchange rate (original currency ➜ local currency) captured at payment time.
    /// </summary>
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// UTC date/time when the payment occurred or was recorded.
    /// </summary>
    public DateTimeOffset DateUtc { get; set; }

    /// <summary>
    /// Business purpose (Transaction settlement, Deposit, Payroll, …).
    /// </summary>
    public PaymentType Type { get; set; }

    /// <summary>
    /// How the money physically moved (cash, card, bank transfer, …).
    /// </summary>
    public PaymentMethod Method { get; set; }

    /// <summary>
    /// Direction of cash flow from company perspective (Income or Expense).
    /// Calculated by the service layer when the record is created.
    /// </summary>
    public PaymentDirection Direction { get; set; }

    /// <summary>
    /// Currency in which payment was made (UZS, USD, ...).
    /// </summary>
    public PaymentCurrency Currency { get; set; }

    /// <summary>
    /// FK to the counter-party whose balance is affected; <c>null</c> for
    /// General or Payroll payments.
    /// </summary>
    public int? PartnerId { get; set; }

    /// <summary>
    /// Navigation to the partner (customer / supplier) – null when not applicable.
    /// </summary>
    public virtual Partner? Partner { get; set; }

    /// <summary>
    /// Breakdown of how the payment amount was allocated to transactions or left as advance.
    /// </summary>
    public virtual ICollection<PaymentAllocation> Allocations { get; set; }

    /// <summary>
    /// Files (cheque images, PDFs, etc.) attached to the payment.
    /// </summary>
    public virtual ICollection<PaymentAttachment> Attachments { get; set; }

    public Payment()
    {
        Allocations = new List<PaymentAllocation>();
        Attachments = new List<PaymentAttachment>();
    }
}
