namespace Ombor.Contracts.Responses.Payment;

/// <summary>An open transaction a payment can settle, with its remaining amount (oldest-first / FIFO).</summary>
/// <param name="Id">The transaction id.</param>
/// <param name="Date">The transaction date.</param>
/// <param name="Type">Sale / Supply / SaleRefund / SupplyRefund.</param>
/// <param name="Total">The transaction's total due.</param>
/// <param name="Paid">How much has been settled so far.</param>
/// <param name="Remaining"><c>Total − Paid</c>.</param>
/// <param name="Number">The transaction's bare document number (the client prepends «№»); null for a legacy row without one.</param>
public sealed record OutstandingTransactionDto(
    int Id,
    DateTimeOffset Date,
    string Type,
    decimal Total,
    decimal Paid,
    decimal Remaining,
    string? Number);
