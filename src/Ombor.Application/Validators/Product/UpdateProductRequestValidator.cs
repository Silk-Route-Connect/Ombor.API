using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Product;

namespace Ombor.Application.Validators.Product;

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Product ID must be greater than zero.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Category ID must be greater than zero.")
            .MustAsync((categoryId, cancellation) => context.Categories.AnyAsync(x => x.Id == categoryId, cancellation))
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
            .WithMessage($"Product SKU must not exceed {ValidationConstants.CodeLength} characters.")
            .MustAsync(async (request, sku, cancellation) =>
                !await context.Products.AnyAsync(p => p.SKU == sku && p.Id != request.Id, cancellation))
            .WithMessage("A product with the same SKU already exists.");

        RuleFor(x => x.Description)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Product description must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Barcode)
            .MaximumLength(ValidationConstants.CodeLength)
            .WithMessage($"Product barcode must not exceed {ValidationConstants.CodeLength} characters.");

        RuleFor(x => x.SupplyPrice)
            .GreaterThan(0)
            .WithMessage("Supply price must be greater than zero.")
            .When(x => x.Type != Contracts.Enums.ProductType.Sale);

        RuleFor(x => x.SalePrice)
            .GreaterThan(0)
            .WithMessage("Sale price must be greater than zero.")
            .GreaterThan(x => x.SupplyPrice)
            .WithMessage("Sale price must be greater than supply price.")
            .When(x => x.Type != Contracts.Enums.ProductType.Supply);

        RuleFor(x => x.RetailPrice)
            .GreaterThan(0)
            .WithMessage("Retail price must be greater than zero.")
            .GreaterThan(x => x.SupplyPrice)
            .WithMessage("Retail price must be greater than supply price.")
            .LessThan(x => x.SalePrice)
            .WithMessage("Retail price must be less than sale price.")
            .When(x => x.Type != Contracts.Enums.ProductType.Supply);

        RuleFor(x => x.QuantityInStock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity in stock must be greater than or equal to zero.");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Low stock threshold must be greater than or equal to zero.");

        RuleFor(x => x.Packaging!)
            .SetValidator(new ProductPackagingValidator())
            .When(x => x.Packaging is not null);
    }
}
