using FluentValidation;
using Ombor.Contracts.Requests.Employee;

namespace Ombor.Application.Validators.Employee;

public sealed class DeleteEmployeeRequestValidator : AbstractValidator<DeleteEmployeeRequest>
{
    public DeleteEmployeeRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid employee ID.");
    }
}
