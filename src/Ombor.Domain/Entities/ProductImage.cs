using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class ProductImage : EntityBase
{
    /// <summary>
    /// Gets or sets the unique name for the product image file.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Gets or sets the name of the product image file.
    /// </summary>
    public required string ImageName { get; set; }

    /// <summary>
    /// Gets or sets the URL of the product image.
    /// </summary>
    public required string OriginalUrl { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URL of the product image.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Gets or sets the product ID this image belongs to.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product associated with this image.
    /// </summary>
    public required virtual Product Product { get; set; }
}
