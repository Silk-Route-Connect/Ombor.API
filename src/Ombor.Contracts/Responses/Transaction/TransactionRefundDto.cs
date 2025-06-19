using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Transaction;

/// <summary>
/// Refund transaction linked to the original transaction.
/// </summary>
public sealed record TransactionRefundDto(
    int Id,
    int RefundedTransactionId,
    TransactionRefundType Type,
    string? Notes,
    string? TransactionNumber,
    decimal TotalDue,
    decimal TotalPaid,
    TransactionLineDto[] Lines,
    TransactionPaymentDto[] Payments);
