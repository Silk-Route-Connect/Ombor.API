using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Report;

/// <summary>
/// Money in and out of wallets in a period: what each wallet held at the start and the end, and what moved in between.
/// Only money that moved through a wallet counts (an advance drawn down moves none). Per wallet and in total:
/// <c>closing = opening + initialBalance + income − expense + transfersIn − transfersOut</c>.
/// </summary>
/// <param name="From">First local calendar day of the period, inclusive.</param>
/// <param name="To">Last local calendar day of the period, inclusive.</param>
/// <param name="Wallets">Each wallet (or the requested one), by name; archived wallets included (rule 31).</param>
/// <param name="ByType">Payments in the period by payment type (every type, zeros included).</param>
/// <param name="Series">One point per local day of the period, oldest first.</param>
/// <param name="Totals">The listed wallets together. Between two listed wallets a transfer adds the same amount to transfers in and out.</param>
public sealed record CashFlowReportDto(
    DateOnly From,
    DateOnly To,
    CashFlowWalletDto[] Wallets,
    CashFlowTypeDto[] ByType,
    CashFlowDayDto[] Series,
    CashFlowTotalsDto Totals);

/// <summary>One wallet's cash flow.</summary>
/// <param name="WalletId">The wallet id.</param>
/// <param name="Name">The wallet name.</param>
/// <param name="Type">Cash / Card / Bank.</param>
/// <param name="IsArchived">Whether the wallet is archived.</param>
/// <param name="Opening">Balance at the start of the period (0 for a wallet created later).</param>
/// <param name="InitialBalance">The opening balance entered when the wallet was created inside the period; 0 otherwise.</param>
/// <param name="Income">Income payments into the wallet.</param>
/// <param name="Expense">Expense payments out of the wallet.</param>
/// <param name="TransfersIn">Transfers received from other wallets.</param>
/// <param name="TransfersOut">Transfers sent to other wallets.</param>
/// <param name="Closing">Balance at the end of the period; for a period ending today, the wallet's balance.</param>
public sealed record CashFlowWalletDto(
    int WalletId,
    string Name,
    string Type,
    bool IsArchived,
    decimal Opening,
    decimal InitialBalance,
    decimal Income,
    decimal Expense,
    decimal TransfersIn,
    decimal TransfersOut,
    decimal Closing);

/// <summary>Payments of one type in the period.</summary>
/// <param name="Type">The payment type.</param>
/// <param name="Income">Money in.</param>
/// <param name="Expense">Money out.</param>
/// <param name="Count">Payments of this type.</param>
public sealed record CashFlowTypeDto(
    PaymentType Type,
    decimal Income,
    decimal Expense,
    int Count);

/// <summary>One local day of the cash flow.</summary>
/// <param name="Date">The local calendar day.</param>
/// <param name="Income">Money in by payments.</param>
/// <param name="Expense">Money out by payments.</param>
/// <param name="TransfersIn">Transfers received.</param>
/// <param name="TransfersOut">Transfers sent.</param>
/// <param name="InitialBalance">Opening balances of wallets created that day.</param>
/// <param name="Closing">Money at the end of the day.</param>
public sealed record CashFlowDayDto(
    DateOnly Date,
    decimal Income,
    decimal Expense,
    decimal TransfersIn,
    decimal TransfersOut,
    decimal InitialBalance,
    decimal Closing);

/// <summary>The listed wallets together (fields as on <see cref="CashFlowWalletDto"/>).</summary>
/// <param name="Opening">Balance at the start of the period.</param>
/// <param name="InitialBalance">Opening balances of wallets created in the period.</param>
/// <param name="Income">Money in by payments.</param>
/// <param name="Expense">Money out by payments.</param>
/// <param name="TransfersIn">Transfers received.</param>
/// <param name="TransfersOut">Transfers sent.</param>
/// <param name="Closing">Balance at the end of the period.</param>
public sealed record CashFlowTotalsDto(
    decimal Opening,
    decimal InitialBalance,
    decimal Income,
    decimal Expense,
    decimal TransfersIn,
    decimal TransfersOut,
    decimal Closing);
