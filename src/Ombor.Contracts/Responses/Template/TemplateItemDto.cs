namespace Ombor.Contracts.Responses.Template;

public sealed record TemplateItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int TemplateId,
    string TemplateName,
    decimal UnitPrice,
    decimal Discount);
