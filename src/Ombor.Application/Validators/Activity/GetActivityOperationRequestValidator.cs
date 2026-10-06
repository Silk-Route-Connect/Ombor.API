using FluentValidation;
using Ombor.Contracts.Requests.Activity;

namespace Ombor.Application.Validators.Activity;

public sealed class GetActivityOperationRequestValidator : AbstractValidator<GetActivityOperationRequest>
{
    public GetActivityOperationRequestValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .WithMessage("Invalid operation ID.");
    }
}
