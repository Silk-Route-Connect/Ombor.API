using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a single item of a <see cref="Template"/>.
/// </summary>
public class TemplateItem : AuditableEntity, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets quantity of the <see cref="TemplateItem"/>.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// When the item was entered in packages, the package size (base units per package) snapshotted from the
    /// product at event time; null when entered in base units. Audit-only (rule 21): <see cref="Quantity"/>
    /// (base units) is server-computed as <c>pack count × PackageSize</c>; the pack count is recoverable as
    /// <c>Quantity / PackageSize</c> and is not stored separately.
    /// </summary>
    public int? PackageSize { get; set; }

    /// <summary>
    /// Gets or sets unit price of the <see cref="TemplateItem"/>.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the discount value of the <see cref="TemplateItem"/>, interpreted by <see cref="DiscountType"/>.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets how <see cref="DiscountAmount"/> is interpreted (rule 37). Defaults to Fixed.
    /// Persisted so a fixed discount survives a template → transaction load without rescaling.
    /// </summary>
    public DiscountType DiscountType { get; set; } = DiscountType.Fixed;

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
