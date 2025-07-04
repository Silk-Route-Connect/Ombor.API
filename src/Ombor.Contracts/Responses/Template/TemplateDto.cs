namespace Ombor.Contracts.Responses.Template;

public sealed record TemplateDto(
    int Id,
    int PartnerId,
    string PartnerName,
    string Name,
    string Type,
    TemplateItemDto[] Items);
