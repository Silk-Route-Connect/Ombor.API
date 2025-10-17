using FluentValidation;
using Ombor.Contracts.Requests.Common;

namespace Ombor.Application.Validators.Common;

public abstract class PagedRequestValidator<T> : AbstractValidator<T> where T : PagedRequest
{
    protected PagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}
