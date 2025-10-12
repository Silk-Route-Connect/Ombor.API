using FluentValidation;
using Ombor.Domain.Common;

namespace Ombor.Application.Validators.Common;

public sealed class IdValidator<TEntity> : AbstractValidator<int> where TEntity : EntityBase
{
    public IdValidator()
    {
        RuleFor(x => x)
            .GreaterThan(0)
            .WithMessage(x => $"Invalid {typeof(TEntity)} ID: {x}.");
    }
}
