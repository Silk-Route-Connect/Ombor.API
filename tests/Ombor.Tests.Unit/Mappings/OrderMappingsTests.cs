using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Order;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using ContractDiscountType = Ombor.Contracts.Enums.DiscountType;
using ContractOrderSource = Ombor.Contracts.Enums.OrderSource;

namespace Ombor.Tests.Unit.Mappings;

public sealed class OrderMappingsTests
{
    private static readonly IReadOnlyDictionary<int, string> NoActors = new Dictionary<int, string>();

    [Fact]
    public void ToDto_ShouldThrow_WhenCustomerIsNull()
    {
        var order = NewOrder(customer: null);

        Assert.Throws<InvalidOperationException>(() => order.ToDto(0m, NoActors));
    }

    [Fact]
    public void LineToDto_ShouldThrow_WhenProductIsNull()
    {
        var line = new OrderLine
        {
            Quantity = 1,
            UnitPrice = 10m,
            Product = null!,
            Order = null!,
        };

        Assert.Throws<InvalidOperationException>(() => line.ToDto());
    }

    [Fact]
    public void ToDto_ShouldMapComputedAndLineFields()
    {
        var customer = new Partner { Id = 7, Name = "Acme", Type = PartnerType.Customer };
        var product = new Product { Id = 3, Name = "Widget", SKU = "SKU-3", Measurement = UnitOfMeasurement.Box, Category = null! };
        var order = NewOrder(customer);
        order.Id = 42;
        order.Lines =
        [
            new OrderLine
            {
                Id = 1,
                ProductId = product.Id,
                Product = product,
                Quantity = 2,
                UnitPrice = 100m,
                Discount = 50m,
                DiscountType = DiscountType.Fixed,
                Order = null!,
            }
        ];

        var dto = order.ToDto(1_234m, NoActors);

        Assert.Equal(42, dto.Id);
        Assert.Equal(7, dto.CustomerId);
        Assert.Equal("Acme", dto.CustomerName);
        Assert.Equal("Customer", dto.CustomerType);
        Assert.Equal(1_234m, dto.CustomerBalance);
        Assert.Null(dto.SaleId);

        var line = Assert.Single(dto.Lines);
        Assert.Equal("Widget", line.ProductName);
        Assert.Equal("SKU-3", line.Sku);
        Assert.Equal("Box", line.Measurement);
        Assert.Equal("Fixed", line.DiscountType);
        // Fixed discount: gross (200) − 50 = 150.
        Assert.Equal(150m, line.Total);
    }

    [Theory]
    // Fixed: gross − discount, clamped to gross.
    [InlineData(DiscountType.Fixed, 50, 150)]
    [InlineData(DiscountType.Fixed, 500, 0)]
    // Percentage: gross × (1 − discount/100), clamped to 100%.
    [InlineData(DiscountType.Percentage, 10, 180)]
    [InlineData(DiscountType.Percentage, 150, 0)]
    public void LineToDto_ShouldApplyRule37(DiscountType type, decimal discount, decimal expectedTotal)
    {
        var product = new Product { Id = 1, Name = "P", SKU = "S", Measurement = UnitOfMeasurement.Unit, Category = null! };
        var line = new OrderLine
        {
            ProductId = product.Id,
            Product = product,
            Quantity = 2,
            UnitPrice = 100m,
            Discount = discount,
            DiscountType = type,
            Order = null!,
        };

        Assert.Equal(expectedTotal, line.ToDto().Total);
    }

    [Fact]
    public void ToDto_ShouldResolveHistoryActorName_AndOrderChronologically()
    {
        var customer = new Partner { Id = 1, Name = "Acme", Type = PartnerType.Customer };
        var order = NewOrder(customer);
        var earlier = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        order.History =
        [
            new OrderStatusEvent { Id = 2, At = earlier.AddHours(1), From = OrderStatus.Pending, To = OrderStatus.Processing, By = 9, Order = null! },
            new OrderStatusEvent { Id = 1, At = earlier, From = null, To = OrderStatus.Pending, By = null, Order = null! },
        ];

        var dto = order.ToDto(0m, new Dictionary<int, string> { [9] = "Jane Doe" });

        Assert.Equal(2, dto.History.Length);
        // Chronological (oldest first): creation event first.
        Assert.Null(dto.History[0].From);
        Assert.Equal("Pending", dto.History[0].To);
        Assert.Null(dto.History[0].By);
        Assert.Equal("Processing", dto.History[1].To);
        Assert.Equal("Jane Doe", dto.History[1].By);
    }

    [Fact]
    public void RequestToEntity_ShouldMapDiscountTypeAndFields()
    {
        var request = new CreateOrderRequest(
            CustomerId: 5,
            Source: ContractOrderSource.OmborWeb,
            WarehouseId: 3,
            DeliveryAddress: "Tashkent",
            DeliveryDate: null,
            DeliveryTime: null,
            Notes: "note",
            Lines:
            [
                new CreateOrderLineRequest(ProductId: 8, Quantity: 4, UnitPrice: 25m, Discount: 10m, DiscountType: ContractDiscountType.Percentage),
            ]);

        var entity = request.ToEntity();

        Assert.Equal(5, entity.CustomerId);
        Assert.Equal(3, entity.WarehouseId);
        Assert.Equal("Tashkent", entity.DeliveryAddress.Text);
        Assert.Equal(OrderStatus.Pending, entity.Status);
        var line = Assert.Single(entity.Lines);
        Assert.Equal(DiscountType.Percentage, line.DiscountType);
        // 100 gross − 10% = 90.
        Assert.Equal(90m, line.TotalPrice);
        Assert.Equal(90m, entity.TotalAmount);
    }

    private static Order NewOrder(Partner? customer) => new()
    {
        OrderNumber = "ORD",
        TotalAmount = 0m,
        DateUtc = DateTimeOffset.UtcNow,
        Status = OrderStatus.Pending,
        Source = OrderSource.OmborWeb,
        CustomerId = customer?.Id ?? 1,
        Customer = customer!,
    };
}
