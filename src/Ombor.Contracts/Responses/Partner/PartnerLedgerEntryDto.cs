namespace Ombor.Contracts.Responses.Partner;

/// <summary>
/// One entry of a partner's derived ledger (newest-first). The running <see cref="Balance"/> over the
/// whole ledger reconciles exactly to the partner's net balance (complexity notes §G).
/// </summary>
/// <param name="Id">The source event id (transaction id, payment id, or 0 for the opening event).</param>
/// <param name="Type">The event type: opening, sale, supply, refund-sale, refund-supply, payment, deposit, withdraw.</param>
/// <param name="Date">When the event occurred.</param>
/// <param name="Delta">The signed effect on the balance (positive = the partner owes us more).</param>
/// <param name="Balance">The running balance after this event.</param>
/// <param name="SourceId">
/// The id of the underlying record to navigate to: the transaction id for sale/supply/refund-* rows,
/// the payment id for payment/deposit/withdraw rows, and null for the opening event.
/// <see cref="Type"/> tells the frontend which kind it is.
/// </param>
/// <param name="Reference">The bare document number of the underlying record — the transaction number for sale/supply/refund-* rows and the payment number for payment/deposit/withdraw rows; null for the opening event and for records without a persisted number (synthetic seed rows).</param>
/// <param name="ItemCount">Number of line items, for transaction events.</param>
/// <param name="Status">Settlement status for transaction events (paid, partial, unpaid) or "done".</param>
/// <param name="WalletName">The wallet the money moved through, for payment events (payment, deposit, withdraw); null otherwise.</param>
/// <param name="WalletType">The wallet's type (e.g. Cash, Card), for payment events; null otherwise.</param>
public sealed record PartnerLedgerEntryDto(
    int Id,
    string Type,
    DateTimeOffset Date,
    decimal Delta,
    decimal Balance,
    int? SourceId,
    string? Reference,
    int? ItemCount,
    string? Status,
    string? WalletName,
    string? WalletType);
