using FluentValidation;
using Ombor.Contracts.Requests.Category;

namespace Ombor.Application.Validators.Category;

public sealed class DeleteCategoryRequestValidator : AbstractValidator<DeleteCategoryRequest>
{
    public DeleteCategoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid category ID.");
    }
}
