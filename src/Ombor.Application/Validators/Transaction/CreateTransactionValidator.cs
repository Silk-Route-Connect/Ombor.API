using FluentValidation;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Application.Validators.Transaction;

public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.PartnerId)
            .GreaterThan(0)
            .WithErrorCode("Invalid Partner ID.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Payment notes must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Payments)
                .NotNull()
                .WithMessage("Payments collection cannot be null.")
                .Must(p => p.All(pc => pc.ExchangeRate > 0))
                .WithMessage("Exchange rate must be positive for all payment components.");

        RuleForEach(x => x.Payments)
            .ChildRules(pc =>
            {
                pc.RuleFor(c => c.Amount)
                .GreaterThan(0m)
                .WithMessage("Payment amount must be positive.");
            });

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("Transaction must contain at least one line item.");
    }
}
