using FluentValidation;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payments;

namespace Ombor.Application.Validators.Payment;

public sealed class CreateTransactionPaymentRequestValidator : AbstractValidator<CreateTransactionPaymentRequest>
{
    public CreateTransactionPaymentRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotNull()
            .GreaterThanOrEqualTo(0)
            .WithMessage("Invalid Transaction ID.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Payment amount must be positive.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Exchange rate must be positive")
            .Equal(1)
            .When(x => x.Currency == PaymentCurrency.USD)
            .WithMessage("Exchange rate must 1 when payment is made in local currency.");
    }
}
