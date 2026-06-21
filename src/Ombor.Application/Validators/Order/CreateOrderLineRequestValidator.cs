using FluentValidation;
using Ombor.Contracts.Requests.Order;

namespace Ombor.Application.Validators.Order;

public sealed class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("All order lines must have a quantity greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("All order lines must have a unit price greater than zero.");

        RuleFor(x => x.DiscountType)
            .IsInEnum()
            .WithMessage("Invalid discount type.");

        // Only the non-negative floor is enforced; an over-large discount (percentage > 100 or a fixed
        // amount above the line gross) is clamped by the line's rule-37 total, per complexity notes §G.
        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Discount.HasValue)
            .WithMessage("Discount cannot be negative.");
    }
}
