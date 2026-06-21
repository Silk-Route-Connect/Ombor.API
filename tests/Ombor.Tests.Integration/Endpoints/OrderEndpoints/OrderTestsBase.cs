using System.Net;
using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.OrderEndpoints;

public abstract class OrderTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper output)
        : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Order;
    protected override string GetUrl(int id) => $"{Routes.Order}/{id}";

    protected async Task<int> CreateCustomerAsync()
    {
        var partner = new Partner
        {
            Name = _faker.Person.FullName,
            Type = PartnerType.Customer,
        };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    protected async Task<int> CreateWarehouseAsync()
    {
        var warehouse = new Warehouse
        {
            Name = $"Warehouse {Guid.NewGuid():N}",
            Location = "Tashkent",
        };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return warehouse.Id;
    }

    protected async Task<int> CreateProductAsync()
    {
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            SalePrice = 100m,
            SupplyPrice = 50m,
            RetailPrice = 90m,
            LowStockThreshold = 10,
            Measurement = UnitOfMeasurement.Unit,
            Type = ProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }

    /// <summary>Seeds stock so a delivery (stock-out) has something to draw from (used in M5b).</summary>
    protected async Task SeedStockAsync(int warehouseId, int productId, int quantity, decimal averageCost = 50m)
    {
        var item = new WarehouseItem
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            AverageCost = averageCost,
            Warehouse = null!,
            Product = null!,
        };
        _context.WarehouseItems.Add(item);
        await _context.SaveChangesAsync();
    }

    /// <summary>Seeds an open receivable Sale so the customer balance is non-zero.</summary>
    protected async Task<int> CreateOpenSaleAsync(int partnerId, decimal due)
    {
        var sale = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = TransactionType.Sale,
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = due,
            TotalPaid = 0m,
            Status = TransactionStatus.Open,
        };
        _context.Transactions.Add(sale);
        await _context.SaveChangesAsync();

        return sale.Id;
    }

    protected static object BuildCreateBody(
        int customerId,
        int productId,
        int quantity = 2,
        decimal unitPrice = 100m,
        int? warehouseId = null,
        decimal? discount = null,
        string discountType = "Fixed")
        => new
        {
            customerId,
            source = "OmborWeb",
            warehouseId,
            deliveryAddress = "Test address",
            notes = "Test order",
            lines = new[]
            {
                new { productId, quantity, unitPrice, discount, discountType },
            },
        };

    protected Task<OrderDto> PostOrderAsync(object body)
        => _client.PostAsync<OrderDto>(Routes.Order, body);

    protected Task<OrderDto> PostStateAsync(int orderId, string action)
        => _client.PostAsync<OrderDto>($"{Routes.Order}/{orderId}/{action}", new { }, HttpStatusCode.OK);

    /// <summary>Creates an order and walks it forward to <paramref name="target"/> via the state endpoints.</summary>
    protected async Task<OrderDto> CreateOrderInStatusAsync(int customerId, int productId, OrderStatus target, int? warehouseId = null)
    {
        var order = await PostOrderAsync(BuildCreateBody(customerId, productId, warehouseId: warehouseId));

        if (target is OrderStatus.Processing or OrderStatus.Shipping or OrderStatus.Delivered)
        {
            order = await PostStateAsync(order.Id, "process");
        }

        if (target is OrderStatus.Shipping or OrderStatus.Delivered)
        {
            order = await PostStateAsync(order.Id, "ship");
        }

        if (target is OrderStatus.Cancelled)
        {
            order = await PostStateAsync(order.Id, "cancel");
        }

        if (target is OrderStatus.Rejected)
        {
            order = await PostStateAsync(order.Id, "reject");
        }

        return order;
    }
}
