namespace Ombor.Contracts.Responses.Template;

public sealed record CreateTemplateResponse(
    int Id,
    string Name,
    string Type,
    TemplateItemDto[] Items);
