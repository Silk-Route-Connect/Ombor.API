using FluentValidation;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payments;

namespace Ombor.Application.Validators.Payment;

public sealed class UpdatePaymentRequestValidator : AbstractValidator<UpdatePaymentRequest>
{
    public UpdatePaymentRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid Payment ID.");

        // NotNull rule is temporary, can be removed when payment
        // can be made for an employee payroll as well.
        RuleFor(x => x.PartnerId)
            .NotNull()
            .GreaterThanOrEqualTo(0)
            .WithMessage("Invalid Partner ID.");

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
