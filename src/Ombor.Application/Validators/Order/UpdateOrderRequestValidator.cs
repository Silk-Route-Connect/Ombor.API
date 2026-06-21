using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Order;

namespace Ombor.Application.Validators.Order;

public sealed class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.Id)
            .SetValidator(new IdValidator<Domain.Entities.Order>());

        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid customer ID: {x.CustomerId}.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Invalid order source.");

        // Tri-state: unspecified or cleared is fine; a supplied value must be a valid id.
        RuleFor(x => x.WarehouseId)
            .Must(w => !w.IsSpecified || w.Value is null or > 0)
            .WithMessage("Invalid warehouse ID.");

        RuleFor(x => x.DeliveryAddress)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .When(x => x.DeliveryAddress is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .When(x => x.Notes is not null);

        RuleFor(x => x.Lines)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one order line is required.");

        RuleForEach(x => x.Lines)
            .SetValidator(new CreateOrderLineRequestValidator());
    }
}
