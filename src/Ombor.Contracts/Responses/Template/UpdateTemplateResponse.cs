using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Template;

public sealed record UpdateTemplateResponse(
    int Id,
    string Name,
    TemplateType Type,
    TemplateItemDto[] Items);
