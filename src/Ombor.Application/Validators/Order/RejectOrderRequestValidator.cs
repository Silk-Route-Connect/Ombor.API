using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Order;

namespace Ombor.Application.Validators.Order;

public sealed class RejectOrderRequestValidator : AbstractValidator<RejectOrderRequest>
{
    public RejectOrderRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .SetValidator(new IdValidator<Domain.Entities.Order>());
    }
}
