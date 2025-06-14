namespace Ombor.Contracts.Responses.Template;

public sealed record TemplateDto(
    int Id,
    string Name,
    string Type,
    TemplateItemDto[] Items);
