namespace Ombor.Contracts.Responses.Template;

/// <summary>A single line of a template (a reusable draft basket for a Sale or Supply).</summary>
/// <param name="Id">The template item id.</param>
/// <param name="ProductId">The product.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Sku">The product SKU, from the product catalogue.</param>
/// <param name="Measurement">The product's unit of measurement.</param>
/// <param name="TemplateId">The owning template id.</param>
/// <param name="TemplateName">The owning template name.</param>
/// <param name="Quantity">The line quantity.</param>
/// <param name="UnitPrice">The unit price.</param>
/// <param name="Discount">The discount value, interpreted per <paramref name="DiscountType"/>.</param>
/// <param name="DiscountType">Whether <paramref name="Discount"/> is a Percentage or a Fixed amount (rule 37).</param>
public sealed record TemplateItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    string Measurement,
    int TemplateId,
    string TemplateName,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    string DiscountType);
