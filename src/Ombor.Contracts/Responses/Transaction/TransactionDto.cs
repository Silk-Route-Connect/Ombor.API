namespace Ombor.Contracts.Responses.Transaction;

/// <summary>
/// A transaction as it appears in the Sales/Supplies list.
/// </summary>
/// <param name="Id">The transaction id.</param>
/// <param name="Number">Human-facing document number, sequential per organization. Rendered bare (the «№» prefix is added client-side).</param>
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

/// <summary>A single line of a transaction (shared by the list and detail views).</summary>
/// <param name="PackageSize">When the line was entered in packages, the package size (base units per package) at entry time; null for a base-unit line. The entered pack count is <paramref name="Quantity"/> ÷ this value; <paramref name="Quantity"/> (base units) stays authoritative (rule 21).</param>
public sealed record TransactionLineDto(
    int Id,
    int ProductId,
    string ProductName,
    int TransactionId,
    decimal UnitPrice,
    decimal Discount,
    string DiscountType,
    decimal Quantity,
    decimal Total,
    int? PackageSize = null);
