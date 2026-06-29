using FluentValidation;
using Ombor.Contracts.Requests.Warehouse;

namespace Ombor.Application.Validators.Warehouse;

public sealed class DeleteWarehouseRequestValidator : AbstractValidator<DeleteWarehouseRequest>
{
    public DeleteWarehouseRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid warehouse ID: {x.Id}.");
    }
}
