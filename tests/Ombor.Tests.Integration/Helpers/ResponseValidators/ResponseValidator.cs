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
    public PartnerValidator Partner => _partner ??= new(context);

    private TemplateValidator? _template;
    public TemplateValidator Template => _template ??= new(context);

    private EmployeeValidator? _employee;
    public EmployeeValidator Employee => _employee ??= new(context);

    private InventoryValidator? _inventory;
    public InventoryValidator Inventory => _inventory ??= new(context);
}
