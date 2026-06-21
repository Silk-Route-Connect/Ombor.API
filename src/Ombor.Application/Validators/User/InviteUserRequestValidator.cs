using FluentValidation;
using Ombor.Contracts.Requests.User;

namespace Ombor.Application.Validators.User;

public sealed class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Contact value is required.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Contact value must not exceed {ValidationConstants.DefaultStringLength} characters.");
    }
}
