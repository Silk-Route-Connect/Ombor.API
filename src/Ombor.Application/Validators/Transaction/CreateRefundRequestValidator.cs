using FluentValidation;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transactions;

namespace Ombor.Application.Validators.Transaction;

public sealed class CreateRefundRequestValidator : AbstractValidator<CreateRefundRequest>
{
    public CreateRefundRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0)
            .WithMessage("Invalid Transaction ID.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Notes must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.TotalPaid)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Payment amount must be positive.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0)
            .WithMessage("Exchange rate must be positive.")
            .Equal(1)
            .When(x => x.Currency == PaymentCurrency.UZS)
            .WithMessage("Exchange rate must be 1 when payment is made in local currency.");

        RuleForEach(r => r.Lines)
            .SetValidator(new RefundTransactionLineValidator());
    }
}
