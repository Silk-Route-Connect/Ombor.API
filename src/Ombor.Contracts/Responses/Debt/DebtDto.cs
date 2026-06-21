namespace Ombor.Contracts.Responses.Debt;

/// <summary>
/// One outstanding transaction in the debts read model. Derived on read from the transaction ledger
/// (no stored entity); the set is complete and consistent with transactions so the client can compute
/// summary cards, by-partner groups, and aging buckets from it (complexity notes §J).
/// </summary>
/// <param name="TransactionId">The underlying transaction's id (the UI deep-links debt rows to it / the partner).</param>
/// <param name="Number">Human-friendly transaction number (provisional, derived from type + id).</param>
/// <param name="Direction">"Receivable" (the partner owes us) or "Payable" (we owe the partner).</param>
/// <param name="TransactionType">Sale, Supply, SaleRefund or SupplyRefund.</param>
/// <param name="PartnerId">The partner this debt is with.</param>
/// <param name="PartnerName">The partner's name.</param>
/// <param name="PartnerCompany">The partner's company, if any.</param>
/// <param name="PartnerType">The partner's type (Customer, Supplier, Both).</param>
/// <param name="Date">The transaction date.</param>
/// <param name="DueDate">When the amount is due, if a due date was set; otherwise null.</param>
/// <param name="Total">The transaction's total due.</param>
/// <param name="Paid">How much has been settled so far.</param>
/// <param name="Remaining">The outstanding amount (<c>Total − Paid</c>).</param>
/// <param name="AgeDays">Days since the transaction date.</param>
/// <param name="OverdueDays">Days past the due date (>0 = overdue); 0 when not overdue or no due date.</param>
public sealed record DebtDto(
    int TransactionId,
    string Number,
    string Direction,
    string TransactionType,
    int PartnerId,
    string PartnerName,
    string? PartnerCompany,
    string PartnerType,
    DateTimeOffset Date,
    DateOnly? DueDate,
    decimal Total,
    decimal Paid,
    decimal Remaining,
    int AgeDays,
    int OverdueDays);
