using FluentValidation;
using Ombor.Contracts.Requests.Inventory;

namespace Ombor.Application.Validators.Inventory;

public sealed class UpdateInventoryRequestValidator : AbstractValidator<UpdateInventoryRequest>
{
    public UpdateInventoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid inventory ID: {x.Id}.");

        RuleFor(x => x.Name)
           .NotEmpty()
           .WithMessage("Inventory name is required.")
           .MaximumLength(ValidationConstants.DefaultStringLength)
           .WithMessage($"Inventory name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Location)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Inventory location must not exceed {ValidationConstants.MaxStringLength} characters.");
    }
}
