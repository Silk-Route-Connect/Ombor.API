using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class Product : AuditableEntity
{
    public required string Name { get; set; }
    public required string SKU { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public decimal SalePrice { get; set; }
    public decimal SupplyPrice { get; set; }
    public int QuantityInStock { get; set; }
    public UnitOfMeasurement Measurement { get; set; }

    public int CategoryId { get; set; }
    public required virtual Category Category { get; set; }
}
