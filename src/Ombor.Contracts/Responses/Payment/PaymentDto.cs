namespace Ombor.Contracts.Responses.Payment;

public sealed record PaymentDto(
    int Id,
    int? PartnerId,
    string? PartnerName,
    string? Notes,
    decimal Amount,
    DateTimeOffset Date,
    string Direction,
    string Type,
    PaymentComponentDto[] Components,
    PaymentAllocationDto[] Allocations);
