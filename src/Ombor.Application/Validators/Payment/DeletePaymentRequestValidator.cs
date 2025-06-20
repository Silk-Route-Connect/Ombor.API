using FluentValidation;
using Ombor.Contracts.Requests.Payments;

namespace Ombor.Application.Validators.Payment;

public sealed class DeletePaymentRequestValidator : AbstractValidator<DeletePaymentRequest>
{
    public DeletePaymentRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid Payment ID.");
    }
}
