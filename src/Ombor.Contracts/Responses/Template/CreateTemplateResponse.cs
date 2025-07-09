namespace Ombor.Contracts.Responses.Template;

public sealed record CreateTemplateResponse(
    int Id,
    int PartnerId,
    string PartnerName,
    string Name,
    string Type,
    TemplateItemDto[] Items);
