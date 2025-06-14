using FluentValidation;
using Ombor.Contracts.Requests.Partner;

namespace Ombor.Application.Validators.Partner;

public sealed class DeletePartnerRequestValidator : AbstractValidator<DeletePartnerRequest>
{
    public DeletePartnerRequestValidator()
    {
        RuleFor(x => x.Id)
             .GreaterThan(0)
             .WithMessage("Invalid partner ID.");
    }
}