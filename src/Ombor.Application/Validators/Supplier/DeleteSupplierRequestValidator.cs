using FluentValidation;
using Ombor.Contracts.Requests.Supplier;

namespace Ombor.Application.Validators.Supplier;

public sealed class DeleteSupplierRequestValidator : AbstractValidator<DeleteSupplierRequest>
{
    public DeleteSupplierRequestValidator()
    {
        RuleFor(x => x.Id)
             .GreaterThan(0)
             .WithMessage("Invalid supplier ID.");
    }
}