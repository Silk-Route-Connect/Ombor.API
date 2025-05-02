using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

/// <summary>
/// Represents a product category containing zero or more products.
/// </summary>
public class Category : EntityBase
{
    /// <summary>
    /// Gets or sets the name of the category.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the category.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the products that belong to this category.
    /// </summary>
    public virtual ICollection<Product> Products { get; set; }

    public Category()
    {
        Products = new HashSet<Product>();
    }
}
