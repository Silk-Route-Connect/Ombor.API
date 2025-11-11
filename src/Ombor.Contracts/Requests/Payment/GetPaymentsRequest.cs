using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payment;

public sealed record GetPaymentsRequest(
    int? PartnerId = null,
    int? EmployeeId = null,
    int? TransactionId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    PaymentType? Type = null,
    PaymentDirection? Direction = null);
