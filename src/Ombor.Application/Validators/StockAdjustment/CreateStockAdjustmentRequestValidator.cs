using FluentValidation;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.StockAdjustment;

namespace Ombor.Application.Validators.StockAdjustment;

public sealed class CreateStockAdjustmentRequestValidator : AbstractValidator<CreateStockAdjustmentRequest>
{
    private static readonly HashSet<string> DecreaseReasons =
        new(StringComparer.OrdinalIgnoreCase) { "Damage", "Expiry", "Theft", "RecountDown", "Other" };

    private static readonly HashSet<string> IncreaseReasons =
        new(StringComparer.OrdinalIgnoreCase) { "Found", "RecountUp", "Other" };

    public CreateStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Invalid warehouse ID.");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Invalid product ID.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Invalid adjustment direction.");

        RuleFor(x => x.Note)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .When(x => x.Note is not null);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason is required.")
            .Must((request, reason) => IsValidReason(request.Direction, reason))
            .WithMessage("The reason is not valid for the adjustment direction.");
    }

    private static bool IsValidReason(StockAdjustmentDirection direction, string reason) => direction switch
    {
        StockAdjustmentDirection.Decrease => DecreaseReasons.Contains(reason),
        StockAdjustmentDirection.Increase => IncreaseReasons.Contains(reason),
        _ => false,
    };
}
