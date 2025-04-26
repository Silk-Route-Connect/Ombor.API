using Ombor.Application.Interfaces;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class ResponseValidator(IApplicationDbContext context)
{
    private CategoryValidator? _category;
    public CategoryValidator Category => _category ??= new(context);
}
