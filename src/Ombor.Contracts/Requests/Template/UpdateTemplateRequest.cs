using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Template;

public sealed record UpdateTemplateRequest(
    int Id,
    string Name,
    TemplateType Type,
    UpdateTemplateItem[] Items);

public sealed record UpdateTemplateItem(
    int Id,
    int ProductId,
    decimal UnitPrice,
    decimal Discount);
