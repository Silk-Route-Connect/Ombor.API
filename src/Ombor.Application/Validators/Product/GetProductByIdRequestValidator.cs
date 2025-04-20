using FluentValidation;
using Ombor.Contracts.Requests.Product;

namespace Ombor.Application.Validators.Product;
public sealed class GetProductByIdRequestValidator : AbstractValidator<GetProductByIdRequest>
{
    public GetProductByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Product ID must be greater than zero.");
    }
}
