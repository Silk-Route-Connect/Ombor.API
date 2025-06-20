using FluentValidation;
using Ombor.Contracts.Requests.Transactions;

namespace Ombor.Application.Validators.Transaction;

public sealed class RefundTransactionLineValidator : AbstractValidator<RefundTransactionLine>
{
    public RefundTransactionLineValidator()
    {
        RuleFor(l => l.ProductId)
            .GreaterThan(0)
            .WithMessage("ProductId must be greater than 0.");

        RuleFor(l => l.UnitPrice)
            .GreaterThan(0)
            .WithMessage("UnitPrice must be greater than 0.");

        RuleFor(l => l.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.");

        RuleFor(l => l.Discount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Discount cannot be negative.")
            .LessThanOrEqualTo(l => l.UnitPrice)
            .WithMessage("Discount cannot exceed UnitPrice.");
    }
}
