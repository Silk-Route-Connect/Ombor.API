namespace Ombor.Contracts.Responses.Payment;

public sealed record PaymentComponentDto(
    int Id,
    string Method,
    string Currency,
    decimal Amount,
    decimal ExchangeRate);
