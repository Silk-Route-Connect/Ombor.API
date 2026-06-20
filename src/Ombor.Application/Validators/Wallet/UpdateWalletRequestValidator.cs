using FluentValidation;
using Ombor.Contracts.Requests.Wallet;

namespace Ombor.Application.Validators.Wallet;

public sealed class UpdateWalletRequestValidator : AbstractValidator<UpdateWalletRequest>
{
    public UpdateWalletRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid wallet id.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Name must not exceed {ValidationConstants.DefaultStringLength} characters.");
    }
}
