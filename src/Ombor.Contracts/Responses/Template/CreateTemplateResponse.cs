using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Template;

public sealed record CreateTemplateResponse(
    int Id,
    string Name,
    TemplateType Type,
    TemplateItemDto[] Items);
