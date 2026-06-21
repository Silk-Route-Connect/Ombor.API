using FluentValidation;
using Ombor.Contracts.Requests.Warehouse;

namespace Ombor.Application.Validators.Warehouse;

public sealed class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator()
    {
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
