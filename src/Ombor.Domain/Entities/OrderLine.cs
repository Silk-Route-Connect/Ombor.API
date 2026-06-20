using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class OrderLine : AuditableEntity, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    public required int Quantity { get; set; }
    public required decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }

    /// <summary>
    /// How <see cref="Discount"/> is interpreted (rule 37). Defaults to Fixed so an order line that
    /// stored its discount before this field existed still computes the same total.
    /// </summary>
    public DiscountType DiscountType { get; set; } = DiscountType.Fixed;

    /// <summary>
    /// Line total after discount (rule 37). Percentage = <c>gross × discount / 100</c>; Fixed = the value itself.
    /// The discount is clamped to the line gross. Single inline expression so it stays EF-translatable.
    /// </summary>
    public decimal TotalPrice =>
        DiscountType == DiscountType.Percentage
            ? (UnitPrice * Quantity) - (UnitPrice * Quantity * ((Discount ?? 0) > 100m ? 100m : (Discount ?? 0)) / 100m)
            : (UnitPrice * Quantity) - ((Discount ?? 0) > UnitPrice * Quantity ? UnitPrice * Quantity : (Discount ?? 0));

    public int OrderId { get; set; }
    public required virtual Order Order { get; set; }

    public int ProductId { get; set; }
    public required virtual Product Product { get; set; }
}
