using FluentValidation;
using Ombor.Contracts.Requests.Wallet;

namespace Ombor.Application.Validators.Wallet;

public sealed class CreateWalletRequestValidator : AbstractValidator<CreateWalletRequest>
{
    public CreateWalletRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MinimumLength(2)
            .WithMessage("Name must be at least 2 characters.")
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Name must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid wallet type.");

        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Opening balance cannot be negative.");
    }
}
