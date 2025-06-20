namespace Ombor.Contracts.Responses.Transaction;

/// <summary>
/// Line item returned to clients, prices and discounts use the local currency.
/// </summary>
public sealed record TransactionLineDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal Total,
    decimal UnitPrice,
    decimal Quantity,
    decimal Discount);
