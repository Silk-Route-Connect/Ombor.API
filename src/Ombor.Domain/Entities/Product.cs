using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class Product : EntityBase
{
    public required string Name { get; set; }
    public required string SKU { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public decimal SalePrice { get; set; }
    public decimal SupplyPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int LowStockThreshold { get; set; }
    public UnitOfMeasurement Measurement { get; set; }
    public DateOnly? ExpireDate { get; set; }

    public int CategoryId { get; set; }
    public required virtual Category Category { get; set; }
}
