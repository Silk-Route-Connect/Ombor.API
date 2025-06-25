using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Transaction;

/// <summary>
/// Full transaction record including lines, payments and refunds.
/// </summary>
public sealed record TransactionDto(
    int Id,
    int PartnerId,
    string PartnerName,
    string? Notes,
    string? TransactionNumber,
    decimal TotalDue,
    decimal TotalPaid,
    DateTimeOffset Date,
    TransactionType Type,
    TransactionStatus Status,
    TransactionLineDto[] Lines,
    TransactionPaymentDto[] Payments,
    TransactionRefundDto[] Refunds);
