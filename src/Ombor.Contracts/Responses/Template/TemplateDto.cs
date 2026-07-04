namespace Ombor.Contracts.Responses.Template;

/// <summary>A reusable template (draft basket) for creating a Sale or Supply.</summary>
/// <param name="Id">The template id.</param>
/// <param name="PartnerId">The partner the template is for.</param>
/// <param name="PartnerName">The partner name.</param>
/// <param name="Name">The template name.</param>
/// <param name="Type">Sale or Supply.</param>
/// <param name="Items">The template line items.</param>
/// <param name="LastUsedAt">When the template was last loaded into a transaction, or null if it has never been used.</param>
public sealed record TemplateDto(
    int Id,
    int PartnerId,
    string PartnerName,
    string Name,
    string Type,
    TemplateItemDto[] Items,
    DateTimeOffset? LastUsedAt);
