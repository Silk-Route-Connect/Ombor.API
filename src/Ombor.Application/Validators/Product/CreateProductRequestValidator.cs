using FluentValidation;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Enums;

namespace Ombor.Application.Validators.Product;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Category ID must be greater than zero.")
            .Must((categoryId) => context.Categories.Any(x => x.Id == categoryId))
            .WithMessage("Invalid category ID.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Product name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.SKU)
            .NotEmpty()
            .WithMessage("Product SKU is required.")
            .MaximumLength(ValidationConstants.CodeLength)
            .WithMessage($"Product SKU must not exceed {ValidationConstants.CodeLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Product description must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Barcode)
            .MaximumLength(ValidationConstants.CodeLength)
            .WithMessage($"Product barcode must not exceed {ValidationConstants.CodeLength} characters.");

        RuleFor(x => x.Measurement)
            .Must(x => Enum.TryParse<UnitOfMeasurement>(x, out _))
            .WithMessage("Invalid measurement type. Valid values are: Piece, Kilogram, Liter, etc.")
            .When(x => !string.IsNullOrEmpty(x.Measurement));

        RuleFor(x => x.SalePrice)
            .GreaterThan(0)
            .WithMessage("Sale price must be greater than zero.");

        RuleFor(x => x.SupplyPrice)
            .GreaterThan(0)
            .WithMessage("Supply price must be greater than zero.");

        RuleFor(x => x.QuantityInStock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity in stock must be greater than or equal to zero.");
    }
}
