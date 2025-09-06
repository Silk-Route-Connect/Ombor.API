namespace Ombor.Contracts.Responses.Transaction;

public sealed record ProductTransactionDto(
    int Id,
    string TransactionType,
    int ProductId,
    string ProductName,
    int PartnerId,
    string PartnerName,
    DateTimeOffset Date,
    decimal Quantity,
    decimal Discount,
    decimal UnitPrice);
