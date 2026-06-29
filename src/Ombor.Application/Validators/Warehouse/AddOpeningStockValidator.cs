using FluentValidation;
using Ombor.Contracts.Requests.Warehouse;

namespace Ombor.Application.Validators.Warehouse;

public sealed class AddOpeningStockValidator : AbstractValidator<AddOpeningStockRequest>
{
    public AddOpeningStockValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0)
            .WithMessage("Warehouse ID must be valid.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Opening stock must contain at least one item.");

        RuleFor(x => x.Note)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .When(x => x.Note is not null);

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .GreaterThan(0)
                    .WithMessage("Product ID must be valid.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Opening stock quantity must be positive.");

                item.RuleFor(i => i.UnitCost)
                    .GreaterThanOrEqualTo(0m)
                    .WithMessage("Unit cost cannot be negative.");
            });
    }
}
