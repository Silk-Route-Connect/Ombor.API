namespace Ombor.Contracts.Responses.Template;

public sealed record UpdateTemplateResponse(
    int Id,
    int PartnerId,
    string PartnerName,
    string Name,
    string Type,
    TemplateItemDto[] Items);
