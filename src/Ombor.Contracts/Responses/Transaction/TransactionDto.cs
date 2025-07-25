namespace Ombor.Contracts.Responses.Transaction;

public sealed record TransactionDto(
    int Id,
    int PartnerId,
    string PartnerName,
    DateTimeOffset Date,
    string Type,
    string Status,
    decimal TotalDue,
    decimal TotalPaid,
    IEnumerable<TransactionLineDto> Lines);

public sealed record TransactionLineDto(
    int Id,
    int ProductId,
    string ProductName,
    int TransactionId,
    decimal UnitPrice,
    decimal Discount,
    decimal Quantity);
