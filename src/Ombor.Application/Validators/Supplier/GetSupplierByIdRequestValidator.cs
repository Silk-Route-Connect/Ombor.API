using FluentValidation;
using Ombor.Contracts.Requests.Supplier;

namespace Ombor.Application.Validators.Supplier;

public sealed class GetSupplierByIdRequestValidator : AbstractValidator<GetSupplierByIdRequest>
{
    public GetSupplierByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid supplier ID.");
    }
}
