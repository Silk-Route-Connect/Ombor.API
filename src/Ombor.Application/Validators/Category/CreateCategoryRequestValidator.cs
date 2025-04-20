using FluentValidation;
using Ombor.Contracts.Requests.Category;

namespace Ombor.Application.Validators.Category;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
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
