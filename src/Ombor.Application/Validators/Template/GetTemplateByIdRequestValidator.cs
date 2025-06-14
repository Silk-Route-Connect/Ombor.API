using FluentValidation;
using Ombor.Contracts.Requests.Template;

namespace Ombor.Application.Validators.Template;

public sealed class GetTemplateByIdRequestValidator : AbstractValidator<GetTemplateByIdRequest>
{
    public GetTemplateByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Invalid Template ID.");
    }
}
