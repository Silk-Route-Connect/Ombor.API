using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Payments;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal interface IPaymentMappings
{
    Payment ToEntity(CreatePaymentRequest request);
    PaymentDto ToDto(Payment payment);
    CreatePaymentResponse ToCreateResponse(Payment payment);
    UpdatePaymentResponse ToUpdateResponse(Payment payment);
}

internal sealed class PaymentMappings(IDateTimeProvider dateTimeProvider) : IPaymentMappings
{
    public Payment ToEntity(CreatePaymentRequest request) =>
        new()
        {
            Notes = request.Notes,
            Amount = request.Amount,
            AmountLocal = request.Amount * request.ExchangeRate,
            ExchangeRate = request.ExchangeRate,
            DateUtc = dateTimeProvider.UtcNow,
            Type = Enum.Parse<Domain.Enums.PaymentType>(request.Type.ToString()),
            Method = Enum.Parse<Domain.Enums.PaymentMethod>(request.Method.ToString()),
            Direction = Enum.Parse<Domain.Enums.PaymentDirection>(request.Direction.ToString()),
            Currency = Enum.Parse<Domain.Enums.PaymentCurrency>(request.Currency.ToString()),
            PartnerId = request.PartnerId,
        };

    public PaymentDto ToDto(Payment payment) =>
        new(Id: payment.Id,
            PartnerId: payment.PartnerId,
            Notes: payment.Notes,
            ExternalReference: payment.ExternalReference,
            Amount: payment.Amount,
            AmountLocal: payment.AmountLocal,
            ExchangeRate: payment.ExchangeRate,
            DateUtc: payment.DateUtc,
            Type: Enum.Parse<Contracts.Enums.PaymentType>(payment.Type.ToString()),
            Method: Enum.Parse<Contracts.Enums.PaymentMethod>(payment.Method.ToString()),
            Direction: Enum.Parse<Contracts.Enums.PaymentDirection>(payment.Direction.ToString()),
            Currency: Enum.Parse<Contracts.Enums.PaymentCurrency>(payment.Currency.ToString()),
            Attachments: GetAttachments(payment),
            Allocations: GetAllocations(payment));

    public CreatePaymentResponse ToCreateResponse(Payment payment) =>
        new(Id: payment.Id,
            PartnerId: payment.PartnerId,
            Notes: payment.Notes,
            ExternalReference: payment.ExternalReference,
            Amount: payment.Amount,
            AmountLocal: payment.AmountLocal,
            ExchangeRate: payment.ExchangeRate,
            DateUtc: payment.DateUtc,
            Type: Enum.Parse<Contracts.Enums.PaymentType>(payment.Type.ToString()),
            Method: Enum.Parse<Contracts.Enums.PaymentMethod>(payment.Method.ToString()),
            Direction: Enum.Parse<Contracts.Enums.PaymentDirection>(payment.Direction.ToString()),
            Currency: Enum.Parse<Contracts.Enums.PaymentCurrency>(payment.Currency.ToString()),
            Attachments: GetAttachments(payment),
            Allocations: GetAllocations(payment));

    public UpdatePaymentResponse ToUpdateResponse(Payment payment) =>
        new(Id: payment.Id,
            PartnerId: payment.PartnerId,
            Notes: payment.Notes,
            ExternalReference: payment.ExternalReference,
            Amount: payment.Amount,
            AmountLocal: payment.AmountLocal,
            ExchangeRate: payment.ExchangeRate,
            DateUtc: payment.DateUtc,
            Type: Enum.Parse<Contracts.Enums.PaymentType>(payment.Type.ToString()),
            Method: Enum.Parse<Contracts.Enums.PaymentMethod>(payment.Method.ToString()),
            Direction: Enum.Parse<Contracts.Enums.PaymentDirection>(payment.Direction.ToString()),
            Currency: Enum.Parse<Contracts.Enums.PaymentCurrency>(payment.Currency.ToString()),
            Attachments: GetAttachments(payment),
            Allocations: GetAllocations(payment));

    private static PaymentAttachmentDto[] GetAttachments(Payment payment) =>
        payment.Attachments
        .Select(x => new PaymentAttachmentDto(
            Id: x.Id,
            PaymentId: x.PaymentId,
            FileName: x.FileName,
            FileUrl: x.FileUrl,
            Description: x.Description))
        .ToArray();

    private static PaymentAllocationDto[] GetAllocations(Payment payment) =>
        payment.Allocations
        .Select(x => new PaymentAllocationDto(
            Id: x.Id,
            TransactionId: x.TransactionId,
            AppliedAmount: x.AppliedAmount,
            Type: Enum.Parse<Contracts.Enums.PaymentAllocationType>(x.Type.ToString())))
        .ToArray();
}
