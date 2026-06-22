using Ombor.Contracts.Responses.Payment;

namespace Ombor.Contracts.Responses.Transaction;

/// <summary>
/// The full detail of a single transaction for its detail page: header, line items, and the
/// payments that settled it. All money figures are server-computed (rule 2).
/// </summary>
/// <param name="Id">The transaction id.</param>
/// <param name="Number">Human-friendly number (provisional, derived from type + id — matches the debts read model).</param>
/// <param name="Type">Sale, Supply, SaleRefund or SupplyRefund.</param>
/// <param name="Direction">"Receivable" (the partner owes us) or "Payable" (we owe the partner) — the money direction, same split as the debts read model. Distinct from <see cref="Type"/>, which drives the sale/supply detail route.</param>
/// <param name="Status">Settlement status (Open, PartiallyPaid, Closed).</param>
/// <param name="Date">When the transaction occurred.</param>
/// <param name="DueDate">When the amount is due, if a due date was set; otherwise null (due on receipt).</param>
/// <param name="PartnerId">The partner this transaction is with.</param>
/// <param name="PartnerName">The partner's name.</param>
/// <param name="PartnerCompany">The partner's company, if any.</param>
/// <param name="PartnerType">The partner's type (Customer, Supplier, Both).</param>
/// <param name="WarehouseId">The warehouse whose stock this transaction moved, if any.</param>
/// <param name="WarehouseName">The warehouse's name, if any.</param>
/// <param name="TotalDue">The transaction's total.</param>
/// <param name="TotalPaid">How much has been settled so far.</param>
/// <param name="Remaining">The outstanding amount (<c>TotalDue − TotalPaid</c>).</param>
/// <param name="Lines">The line items.</param>
/// <param name="Payments">The payments that settled this transaction (newest-first); empty when nothing has been paid.</param>
/// <param name="OriginalTransactionId">For refunds, the transaction this one reverses; otherwise null.</param>
/// <param name="RefundReason">For refunds, why it was issued; otherwise null.</param>
public sealed record TransactionDetailDto(
    int Id,
    string Number,
    string Type,
    string Direction,
    string Status,
    DateTimeOffset Date,
    DateOnly? DueDate,
    int PartnerId,
    string PartnerName,
    string? PartnerCompany,
    string PartnerType,
    int? WarehouseId,
    string? WarehouseName,
    decimal TotalDue,
    decimal TotalPaid,
    decimal Remaining,
    IReadOnlyList<TransactionLineDto> Lines,
    IReadOnlyList<TransactionPaymentDto> Payments,
    int? OriginalTransactionId,
    string? RefundReason);
