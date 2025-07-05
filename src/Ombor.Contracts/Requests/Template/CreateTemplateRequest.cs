using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Template;

public sealed record CreateTemplateRequest(
    int PartnerId,
    string Name,
    TemplateType Type,
    CreateTemplateItem[] Items);

public sealed record CreateTemplateItem(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal Discount);
