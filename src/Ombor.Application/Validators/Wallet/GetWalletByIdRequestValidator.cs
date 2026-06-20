using FluentValidation;
using Ombor.Contracts.Requests.Wallet;

namespace Ombor.Application.Validators.Wallet;

public sealed class GetWalletByIdRequestValidator : AbstractValidator<GetWalletByIdRequest>
{
    public GetWalletByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid wallet id.");
    }
}
