using FluentValidation;
using Ombor.Contracts.Requests.Product;

namespace Ombor.Application.Validators.Product;

public sealed class DeleteProductRequestValidator : AbstractValidator<DeleteProductRequest>
{
    public DeleteProductRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Product ID must be greater than zero.");
    }
}
