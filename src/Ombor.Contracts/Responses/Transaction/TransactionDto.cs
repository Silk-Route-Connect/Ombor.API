namespace Ombor.Contracts.Responses.Transaction;

/// <summary>
/// A transaction as it appears in the Sales/Supplies list.
/// </summary>
/// <param name="Id">The transaction id.</param>
/// <param name="Number">Human-friendly document number (provisional, derived from type + id — matches the detail view and the debts read model).</param>
/// <param name="PartnerId">The partner this transaction is with.</param>
/// <param name="PartnerName">The partner's name.</param>
/// <param name="Date">When the transaction occurred.</param>
/// <param name="Type">Sale, Supply, SaleRefund or SupplyRefund.</param>
/// <param name="Status">Settlement status (Open, PartiallyPaid, Closed), or Overdue when a non-closed transaction is past its due date — computed on read (rule 2).</param>
/// <param name="TotalDue">The transaction's total.</param>
/// <param name="TotalPaid">How much has been settled so far.</param>
/// <param name="Lines">The line items.</param>
/// <param name="OriginalTransactionId">For refunds, the id of the transaction this one reverses; otherwise null.</param>
/// <param name="OriginalTransactionNumber">For refunds, the document number of the transaction this one reverses; otherwise null.</param>
/// <param name="RefundReason">For refunds, why it was issued; otherwise null.</param>
public sealed record TransactionDto(
    int Id,
    string Number,
    int PartnerId,
    string PartnerName,
    DateTimeOffset Date,
    string Type,
    string Status,
    decimal TotalDue,
    decimal TotalPaid,
    IEnumerable<TransactionLineDto> Lines,
    int? OriginalTransactionId = null,
    string? OriginalTransactionNumber = null,
    string? RefundReason = null);

public sealed record TransactionLineDto(
    int Id,
    int ProductId,
    string ProductName,
    int TransactionId,
    decimal UnitPrice,
    decimal Discount,
    string DiscountType,
    decimal Quantity,
    decimal Total);
