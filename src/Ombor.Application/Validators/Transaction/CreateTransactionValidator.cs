using FluentValidation;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Application.Validators.Transaction;

public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.PartnerId)
            .GreaterThan(0)
            .WithMessage("Invalid Partner ID.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Notes must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("Transaction must contain at least one line item.");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Paid amount cannot be negative.");

        RuleFor(x => x.WalletId)
            .NotNull()
            .When(x => x.PaidAmount > 0m)
            .WithMessage("A wallet is required when a payment is made.");

        RuleForEach(x => x.Settlements)
            .ChildRules(settlement =>
                settlement.RuleFor(s => s.Amount)
                    .GreaterThan(0m)
                    .WithMessage("Settlement amount must be positive."));

        RuleFor(x => x.OriginalTransactionId)
            .NotNull()
            .When(x => x.Type == Contracts.Enums.TransactionType.SaleRefund || x.Type == Contracts.Enums.TransactionType.SupplyRefund)
            .WithMessage("OriginalTransactionId is required for refund transactions.");
    }
}
