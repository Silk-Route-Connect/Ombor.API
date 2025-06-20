using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Payments;

/// <summary>
/// Query parameters for the <c>GET /payments</c> endpoint.
/// All properties are optional; properties that are null are ignored.
/// </summary>
public sealed record GetPaymentsRequest(
    string? SearchTerm,
    int? PartnerId,
    PaymentType? Type);
