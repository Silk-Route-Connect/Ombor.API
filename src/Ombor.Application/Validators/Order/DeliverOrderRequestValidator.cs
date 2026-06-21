using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Order;

namespace Ombor.Application.Validators.Order;

public sealed class DeliverOrderRequestValidator : AbstractValidator<DeliverOrderRequest>
{
    public DeliverOrderRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .SetValidator(new IdValidator<Domain.Entities.Order>());

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0)
            .WithMessage("A warehouse is required to deliver an order.");
    }
}
