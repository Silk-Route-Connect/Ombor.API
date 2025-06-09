using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Template;

public sealed record CreateTemplateRequest(
    string Name,
    TemplateType Type,
    CreateTemplateItem[] Items);

public sealed record CreateTemplateItem(
    int ProductId,
    decimal UnitPrice,
    decimal Discount);
