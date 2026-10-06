using FluentValidation;
using Ombor.Contracts.Requests.Activity;

namespace Ombor.Application.Validators.Activity;

public sealed class GetActivityRequestValidator : AbstractValidator<GetActivityRequest>
{
    public const int MaxPageSize = 100;

    public GetActivityRequestValidator()
    {
        RuleFor(x => x.EntityKind)
            .IsInEnum()
            .When(x => x.EntityKind.HasValue)
            .WithMessage("Invalid entity kind.");

        RuleFor(x => x.EntityId)
            .GreaterThan(0)
            .When(x => x.EntityId.HasValue)
            .WithMessage(x => $"Invalid entity ID: {x.EntityId}.");

        RuleFor(x => x.EntityKind)
            .NotNull()
            .When(x => x.EntityId.HasValue)
            .WithMessage("An entity ID needs its entity kind.");

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .When(x => x.UserId.HasValue)
            .WithMessage(x => $"Invalid user ID: {x.UserId}.");

        RuleFor(x => x.Action)
            .IsInEnum()
            .When(x => x.Action.HasValue)
            .WithMessage("Invalid action.");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'From' must be on or before 'to'.");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be 1 or more.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"Page size must be between 1 and {MaxPageSize}.");
    }
}
