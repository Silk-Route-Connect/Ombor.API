using FluentValidation;
using Ombor.Contracts.Requests.Transfer;

namespace Ombor.Application.Validators.Transfer;

public sealed class CreateTransferValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferValidator()
    {
        RuleFor(x => x.FromInventoryId)
            .GreaterThan(0)
            .WithMessage("Source warehouse ID must be valid.");

        RuleFor(x => x.ToInventoryId)
            .GreaterThan(0)
            .WithMessage("Destination warehouse ID must be valid.");

        RuleFor(x => x.ToInventoryId)
            .NotEqual(x => x.FromInventoryId)
            .WithMessage("Source and destination warehouses must be different.");

        RuleFor(x => x.Notes)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Notes must not exceed {ValidationConstants.MaxStringLength} characters.");

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("Transfer must contain at least one line.");

        RuleForEach(x => x.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.ProductId)
                    .GreaterThan(0)
                    .WithMessage("Product ID must be valid.");

                line.RuleFor(l => l.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Transfer quantity must be positive.");
            });
    }
}
