using FluentValidation;
using Ombor.Contracts.Common;

namespace Ombor.Application.Validators.Product;

public sealed class ProductPackagingValidator : AbstractValidator<ProductPackagingDto>
{
    public ProductPackagingValidator()
    {
        RuleFor(x => x.Size)
            .GreaterThan(1)
            .WithMessage("Packaging size must be greater than 1.");

        RuleFor(x => x.Label)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Packaging label must not exceed {ValidationConstants.DefaultStringLength} characters.")
            .When(x => !string.IsNullOrEmpty(x.Label));

        RuleFor(x => x.Barcode)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Packaging barcode must not exceed {ValidationConstants.DefaultStringLength} characters.")
            .When(x => !string.IsNullOrEmpty(x.Barcode));
    }
}
