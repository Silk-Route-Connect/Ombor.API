using FluentValidation;
using Ombor.Contracts.Requests.Order;

namespace Ombor.Application.Validators.Order;

public sealed class GetOrdersRequestValidator : AbstractValidator<GetOrdersRequest>
{
    public GetOrdersRequestValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(ValidationConstants.DefaultStringLength)
            .WithMessage($"Search term must not exceed {ValidationConstants.DefaultStringLength} characters.");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .When(x => x.CustomerId.HasValue)
            .WithMessage(x => $"Invalid partner ID: {x.CustomerId}.");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("'From date' must be less than or equal to 'to date'.");
    }
}
