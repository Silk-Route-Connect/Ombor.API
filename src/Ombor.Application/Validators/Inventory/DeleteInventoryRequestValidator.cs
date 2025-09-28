using FluentValidation;
using Ombor.Contracts.Requests.Inventory;

namespace Ombor.Application.Validators.Inventory;

public sealed class DeleteInventoryRequestValidator : AbstractValidator<DeleteInventoryRequest>
{
    public DeleteInventoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid inventory ID: {x.Id}.");
    }
}
