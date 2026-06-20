using FluentValidation;
using Ombor.Contracts.Requests.Wallet;

namespace Ombor.Application.Validators.Wallet;

public sealed class CreateWalletTransferRequestValidator : AbstractValidator<CreateWalletTransferRequest>
{
    public CreateWalletTransferRequestValidator()
    {
        RuleFor(x => x.FromWalletId)
            .GreaterThan(0)
            .WithMessage("Invalid source wallet.");

        RuleFor(x => x.ToWalletId)
            .GreaterThan(0)
            .WithMessage("Invalid destination wallet.")
            .NotEqual(x => x.FromWalletId)
            .WithMessage("Source and destination wallets must differ.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Transfer amount must be greater than zero.");
    }
}
