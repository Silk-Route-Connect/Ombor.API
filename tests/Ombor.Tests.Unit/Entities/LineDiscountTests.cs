using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Unit.Entities;

/// <summary>
/// Verifies the rule-37 line-total recompute on the line entities: percentage vs fixed,
/// and the clamp that stops a discount from exceeding the line gross.
/// </summary>
public sealed class LineDiscountTests
{
    [Theory]
    // unitPrice, quantity, discount, type, expectedTotal
    [InlineData(100, 2, 10, DiscountType.Percentage, 180)]   // 10% off 200
    [InlineData(100, 2, 0, DiscountType.Percentage, 200)]    // no discount
    [InlineData(100, 2, 150, DiscountType.Percentage, 0)]    // >100% clamps to the whole line
    [InlineData(100, 2, 50, DiscountType.Fixed, 150)]        // 50 off 200
    [InlineData(100, 2, 200, DiscountType.Fixed, 0)]         // exactly the line
    [InlineData(100, 2, 500, DiscountType.Fixed, 0)]         // fixed exceeds line -> clamped to gross
    public void TransactionLine_Total_AppliesRule37(
        decimal unitPrice, decimal quantity, decimal discount, DiscountType type, decimal expected)
    {
        var line = new TransactionLine
        {
            UnitPrice = unitPrice,
            Quantity = quantity,
            Discount = discount,
            DiscountType = type,
            Product = null!,
            Transaction = null!,
        };

        Assert.Equal(expected, line.Total);
    }

    [Theory]
    [InlineData(100, 2, 10, DiscountType.Percentage, 180)]
    [InlineData(100, 2, 50, DiscountType.Fixed, 150)]
    [InlineData(100, 2, 500, DiscountType.Fixed, 0)]         // fixed exceeds line -> clamped
    [InlineData(100, 2, 150, DiscountType.Percentage, 0)]    // >100% clamps
    public void OrderLine_TotalPrice_AppliesRule37(
        decimal unitPrice, int quantity, decimal discount, DiscountType type, decimal expected)
    {
        var line = new OrderLine
        {
            UnitPrice = unitPrice,
            Quantity = quantity,
            Discount = discount,
            DiscountType = type,
            Order = null!,
            Product = null!,
        };

        Assert.Equal(expected, line.TotalPrice);
    }

    [Fact]
    public void Lines_DefaultDiscountType_PreservesLegacySemantics()
    {
        var transactionLine = new TransactionLine { UnitPrice = 1, Quantity = 1, Product = null!, Transaction = null! };
        var orderLine = new OrderLine { UnitPrice = 1, Quantity = 1, Order = null!, Product = null! };
        var templateItem = new TemplateItem { Product = null!, Template = null! };

        Assert.Equal(DiscountType.Percentage, transactionLine.DiscountType); // legacy transaction lines were percentage
        Assert.Equal(DiscountType.Fixed, orderLine.DiscountType);            // legacy order lines subtracted a fixed amount
        Assert.Equal(DiscountType.Fixed, templateItem.DiscountType);         // legacy template items stored a fixed amount
    }
}
