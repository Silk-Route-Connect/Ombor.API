using FluentValidation;
using Ombor.Contracts.Requests.Order;

namespace Ombor.Application.Validators.Order;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid customer ID: {x.CustomerId}.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid order source.");

        RuleFor(x => x.DeliveryAddress)
            .NotNull()
            .WithMessage("Delivery address is required.");

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one order line is required.")
            .Must(lines => lines.All(line => line.Quantity > 0))
            .WithMessage("All order lines must have a quantity greater than zero.")
            .Must(lines => lines.All(line => line.UnitPrice > 0))
            .WithMessage("All order lines must have a unit price greater than zero.")
            .Must(lines => lines.All(lines => lines.UnitPrice >= lines.Discount))
            .WithMessage("All order lines must have a unit price greater than discount.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .When(x => x.Notes is not null);
    }
}
