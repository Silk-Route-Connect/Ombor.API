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

        RuleForEach(x => x.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.Quantity)
                    .GreaterThan(0m)
                    .WithMessage("Transaction line quantity must be greater than zero.");

                // Guard each line's discount type: an omitted/0/invalid value would otherwise fail deep in
                // the enum mapper as a 500. This mirrors the order-line validator and returns a clean 400.
                line.RuleFor(l => l.DiscountType)
                    .IsInEnum()
                    .WithMessage("Invalid discount type.");

                // Rule 37 clamps only positive discounts, so a negative value would act as a surcharge that
                // inflates TotalDue; a percentage above 100 is nonsensical. Reject both with a 400 rather
                // than silently clamping. (Discount is a non-nullable decimal here, so the floor is unconditional.)
                line.RuleFor(l => l.Discount)
                    .GreaterThanOrEqualTo(0m)
                    .WithMessage("Discount cannot be negative.");

                line.RuleFor(l => l.Discount)
                    .LessThanOrEqualTo(100m)
                    .When(l => l.DiscountType == Contracts.Enums.DiscountType.Percentage)
                    .WithMessage("A percentage discount cannot exceed 100.");
            });

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
            .When(IsRefund)
            .WithMessage("OriginalTransactionId is required for refund transactions.");

        RuleFor(x => x.RefundReason)
            .NotEmpty()
            .When(IsRefund)
            .WithMessage("A reason is required for refund transactions.")
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Refund reason must not exceed {ValidationConstants.MaxStringLength} characters.");
    }

    private static bool IsRefund(CreateTransactionRequest request)
        => request.Type is Contracts.Enums.TransactionType.SaleRefund or Contracts.Enums.TransactionType.SupplyRefund;
}
