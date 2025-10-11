using FluentValidation;
using Ombor.Contracts.Requests.Order;

namespace Ombor.Application.Validators.Order;

public sealed class GetOrderByIdRequestValidator : AbstractValidator<GetOrderByIdRequest>
{
    public GetOrderByIdRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid order ID: {x.OrderId}.");
    }
}
