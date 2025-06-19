using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payments;

public sealed record GetPaymentsRequest(
    string? SearchTerm,
    int? PartnerId,
    PaymentType? Type);
