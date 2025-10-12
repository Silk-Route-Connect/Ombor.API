using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Order;

namespace Ombor.Application.Validators.Order;

public sealed class ReturnOrderRequestValidator : AbstractValidator<ReturnOrderRequest>
{
    public ReturnOrderRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .SetValidator(new IdValidator<Domain.Entities.Order>());
    }
}
