using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class ResponseValidator(IApplicationDbContext context, FileSettings fileSettings, string webRootPath)
{
    private CategoryValidator? _category;
    public CategoryValidator Category => _category ??= new(context);

    private ProductValidator? _product;
    public ProductValidator Product => _product ??= new(context, fileSettings, webRootPath);

    private PartnerValidator? _partner;
    public PartnerValidator partner => _partner ??= new(context);
}
