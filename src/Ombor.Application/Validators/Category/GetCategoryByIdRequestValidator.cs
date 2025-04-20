using FluentValidation;
using Ombor.Contracts.Requests.Category;

namespace Ombor.Application.Validators.Category;

public sealed class GetCategoryByIdRequestValidator : AbstractValidator<GetCategoryByIdRequest>
{
    public GetCategoryByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid category ID.");
    }
}
