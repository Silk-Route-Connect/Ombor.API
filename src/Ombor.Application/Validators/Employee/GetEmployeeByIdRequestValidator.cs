using FluentValidation;
using Ombor.Contracts.Requests.Employee;

namespace Ombor.Application.Validators.Employee;

public sealed class GetEmployeeByIdRequestValidator : AbstractValidator<GetEmployeeByIdRequest>
{
    public GetEmployeeByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid employee ID.");
    }
}
