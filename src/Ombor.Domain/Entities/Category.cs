using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class Category : EntityBase
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; }

    public Category()
    {
        Products = [];
    }
}
