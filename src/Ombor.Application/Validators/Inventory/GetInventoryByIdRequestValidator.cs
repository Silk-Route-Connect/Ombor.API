using FluentValidation;
using Ombor.Contracts.Requests.Inventory;

namespace Ombor.Application.Validators.Inventory;

public sealed class GetInventoryByIdRequestValidator : AbstractValidator<GetInventoryByIdRequest>
{
    public GetInventoryByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid inventory ID.");
    }
}