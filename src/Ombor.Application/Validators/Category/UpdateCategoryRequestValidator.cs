using FluentValidation;
using Ombor.Contracts.Requests.Category;

namespace Ombor.Application.Validators.Category;

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid category ID.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Description must not exceed {ValidationConstants.MaxStringLength} characters.");
    }
}
