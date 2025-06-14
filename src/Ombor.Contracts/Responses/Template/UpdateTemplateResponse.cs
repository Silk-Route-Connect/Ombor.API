namespace Ombor.Contracts.Responses.Template;

public sealed record UpdateTemplateResponse(
    int Id,
    string Name,
    string Type,
    TemplateItemDto[] Items);
