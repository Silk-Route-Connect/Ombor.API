using FluentValidation;
using Ombor.Application.Validators.Common;
using Ombor.Contracts.Requests.Product;

namespace Ombor.Application.Validators.Product;

public sealed class GetProductsRequestValidator : PagedRequestValidator<GetProductsRequest>
{
    public GetProductsRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .When(x => x.CategoryId.HasValue)
            .WithMessage("Category ID must be greater than zero.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("Minimum price must be greater than or equal to zero.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("Maximum price must be greater than or equal to zero.");

        RuleFor(x => x.MaxPrice)
            .GreaterThan(x => x.MinPrice ?? 0)
            .When(x => x.MaxPrice.HasValue && x.MinPrice.HasValue)
            .WithMessage("Maximum price must be greater than minimum price.");

        RuleFor(x => x)
            .Must(x => x.MinPrice == null || x.MaxPrice == null || x.MinPrice <= x.MaxPrice)
            .WithMessage("Price range is invalid. Minimum price cannot exceed maximum price.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithMessage("Invalid product type.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm))
            .WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.SortBy)
            .MustBeValidSortOption(
                "name_asc",
                "name_desc",
                "sku_asc",
                "sku_desc",
                "barcode_asc",
                "barcode_desc",
                "saleprice_asc",
                "saleprice_desc",
                "supplyprice_asc",
                "supplyprice_desc",
                "retailprice_asc",
                "retailprice_desc",
                "quantitystock_asc",
                "quantitystock_desc",
                "lowstock_asc",
                "lowstock_desc",
                "type_asc",
                "type_desc");
    }
}
