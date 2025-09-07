using FluentValidation;
using Ombor.Contracts.Requests.Product;

namespace Ombor.Application.Validators.Product;

public sealed class GetProductTransactionsValidator : AbstractValidator<GetProductTransactionsRequest>
{
    public GetProductTransactionsValidator()
    {
        RuleFor(x => x.Id)
            .NotNull()
            .GreaterThanOrEqualTo(1)
            .WithMessage(x => $"Invalid Product ID: {x.Id}.");
    }
}
