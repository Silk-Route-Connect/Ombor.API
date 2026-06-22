namespace Ombor.Contracts.Responses.Wallet;

/// <summary>
/// A single balance-affecting event in a wallet's running ledger (derived, newest-first).
/// </summary>
/// <param name="Id">Identifier of the source event (opening balance, transfer, …).</param>
/// <param name="Date">When the event occurred.</param>
/// <param name="Kind">Operation kind: Opening / Payment / Deposit / Expense / Withdrawal / Transfer.</param>
/// <param name="Direction">Whether money came In or went Out.</param>
/// <param name="PaymentNumber">Human-friendly payment number, when the event is a payment.</param>
/// <param name="Party">The counterparty (partner / other wallet), when applicable.</param>
/// <param name="Amount">The amount moved (positive magnitude).</param>
/// <param name="BalanceAfter">Computed running balance after this event.</param>
/// <param name="TransferId">The transfer this row belongs to, when the event is a transfer.</param>
/// <param name="PaymentId">The payment this row belongs to, when the event is a payment-kind operation.</param>
public sealed record WalletOperationDto(
    int Id,
    DateTimeOffset Date,
    string Kind,
    string Direction,
    string? PaymentNumber,
    string? Party,
    decimal Amount,
    decimal BalanceAfter,
    int? TransferId,
    int? PaymentId);
