using FluentValidation;
using Ombor.Contracts.Enums;
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

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Discount.HasValue)
            .WithMessage("Discount cannot be negative.");

        // A percentage discount above 100 makes no sense; the entity clamps it, but reject it up front.
        RuleFor(x => x.Discount)
            .LessThanOrEqualTo(100)
            .When(x => x.Discount.HasValue && x.DiscountType == DiscountType.Percentage)
            .WithMessage("A percentage discount cannot exceed 100.");
    }
}
