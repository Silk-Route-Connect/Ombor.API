namespace Ombor.Contracts.Responses.Transaction;

/// <summary>
/// One transaction line of a product (the product detail «Транзакции» tab).
/// </summary>
/// <param name="DiscountType">How <paramref name="Discount"/> is read (rule 37): «Percentage» or «Fixed» per-line amount — clients compute the line net from it.</param>
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
    decimal UnitPrice,
    string DiscountType);
