using FluentValidation;
using Ombor.Contracts.Requests.Warehouse;

namespace Ombor.Application.Validators.Warehouse;

public sealed class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid warehouse ID: {x.Id}.");

        RuleFor(x => x.Name)
           .NotEmpty()
           .WithMessage("Warehouse name is required.")
           .MaximumLength(ValidationConstants.DefaultStringLength)
           .WithMessage($"Warehouse name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Location)
            .MaximumLength(ValidationConstants.MaxStringLength)
            .WithMessage($"Warehouse location must not exceed {ValidationConstants.MaxStringLength} characters.");
    }
}
