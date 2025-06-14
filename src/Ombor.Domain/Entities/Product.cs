using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a product with pricing, stock, and categorization information.
/// </summary>
public class Product : EntityBase
{
    /// <summary>Gets or sets the product's name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the Stock‑Keeping Unit identifier. Value is unique across all products.</summary>
    public required string SKU { get; set; }

    /// <summary>Gets or sets an optional description of the product.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets an optional barcode value.</summary>
    public string? Barcode { get; set; }

    /// <summary>Gets or sets the sale price.</summary>
    public decimal SalePrice { get; set; }

    /// <summary>Gets or sets the supply cost.</summary>
    public decimal SupplyPrice { get; set; }

    /// <summary>Gets or sets the retail price.</summary>
    public decimal RetailPrice { get; set; }

    /// <summary>Gets or sets the current quantity in stock.</summary>
    public int QuantityInStock { get; set; }

    /// <summary>Gets or sets the threshold below which stock is considered low.</summary>
    public int LowStockThreshold { get; set; }

    /// <summary>Gets or sets the unit of measurement for the product.</summary>
    public UnitOfMeasurement Measurement { get; set; }

    /// <summary>Gets or sets type of the product.</summary>
    public ProductType Type { get; set; }

    /// <summary>Gets or sets the foreign key to the Category entity.</summary>
    public int CategoryId { get; set; }

    /// <summary>Gets or sets the <see cref="Entities.Category"/> this product belongs to.</summary>
    public required virtual Category Category { get; set; }

    /// <summary>Gets or sets collection of <see cref="ProductImage"/>. </summary>
    public virtual List<ProductImage> Images { get; set; } = [];

    /// <summary>Gets or sets collection of <see cref="TemplateItem"/>. </summary>
    public virtual List<TemplateItem> TemplateItems { get; set; } = [];
}
