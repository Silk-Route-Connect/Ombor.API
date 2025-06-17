using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a single item of a <see cref="Template"/>.
/// </summary>
public class TemplateItem : AuditableEntity
{
    /// <summary>
    /// Gets or sets quantity of the <see cref="TemplateItem"/>.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets unit price of the <see cref="TemplateItem"/>.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets discount amount (not in percentage) of the <see cref="TemplateItem"/>.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets Product ID of the <see cref="TemplateItem"/>.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets Product of the <see cref="TemplateItem"/>.
    /// </summary>
    public required virtual Product Product { get; set; }

    /// <summary>
    /// Gets or sets Template ID of the <see cref="TemplateItem"/>.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Gets or sets Template of the <see cref="TemplateItem"/>.
    /// </summary>
    public required virtual Template Template { get; set; }
}
