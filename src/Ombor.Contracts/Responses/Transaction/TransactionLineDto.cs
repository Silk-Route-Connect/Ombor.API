namespace Ombor.Contracts.Responses.Transaction;

public sealed record TransactionLineDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    decimal Quantity,
    decimal Discount);
