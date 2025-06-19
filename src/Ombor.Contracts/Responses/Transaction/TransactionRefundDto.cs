using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Transaction;

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
