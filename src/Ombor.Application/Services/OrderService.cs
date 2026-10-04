using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Abstractions;
using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using DomainOrderStatus = Ombor.Domain.Enums.OrderStatus;

namespace Ombor.Application.Services;

internal sealed class OrderService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser,
    INumberSequenceAllocator allocator,
    IOrganizationWriteLock writeLock,
    OrderQueries queries) : IOrderService
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        await validator.ValidateAndThrowAsync(request);
        await EnsureReferencesOwnedAsync(request.CustomerId, request.WarehouseId, request.Lines);

        var entity = request.ToEntity();
        entity.History.Add(new OrderStatusEvent
        {
            At = DateTimeOffset.UtcNow,
            From = null,
            To = entity.Status,
            By = currentUser.UserId,
            Order = entity,
        });

        // Allocate the number and insert in one transaction so a rolled-back create leaves no gap (rule 4).
        await using var databaseTransaction = await context.Database.BeginTransactionAsync();

        entity.OrderNumber = await allocator.AllocateAsync(Ombor.Domain.Enums.NumberSeriesType.Order);
        context.Orders.Add(entity);
        await context.SaveChangesAsync();

        await databaseTransaction.CommitAsync();

        return await queries.GetProjectedOrThrowAsync(entity.Id);
    }

    public async Task<OrderDto[]> GetAsync(GetOrdersRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await queries.GetAsync(request);
    }

    public async Task<OrderDto> GetByIdAsync(GetOrderByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await queries.GetProjectedOrThrowAsync(request.OrderId);
    }

    public async Task<OrderDto> UpdateAsync(UpdateOrderRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // Under the write lock like every status change: an edit can't land on an order a parallel request just
        // shipped or delivered.
        await using var write = await writeLock.BeginOrgWriteAsync();

        await EnsureReferencesOwnedAsync(
            request.CustomerId,
            request.WarehouseId.IsSpecified ? request.WarehouseId.Value : null,
            request.Lines);

        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.Id)
            ?? throw new EntityNotFoundException<Order>(request.Id);

        // Editable only while the order is still open (before it has shipped). An edit is not a transition,
        // so it appends no history event.
        if (order.Status is not (DomainOrderStatus.Pending or DomainOrderStatus.Processing))
        {
            throw new ValidationException($"Order {order.Id} cannot be edited in status {order.Status}.");
        }

        order.CustomerId = request.CustomerId;
        order.Source = Enum.Parse<Domain.Enums.OrderSource>(request.Source.ToString(), ignoreCase: true);
        order.Notes = request.Notes;
        // Only the free-text changes here; any dormant coordinates are preserved.
        order.DeliveryAddress.Text = request.DeliveryAddress;
        order.DeliveryDate = request.DeliveryDate;
        order.DeliveryTime = request.DeliveryTime;

        // Tri-state warehouse: absent = keep, null = clear, value = set.
        if (request.WarehouseId.IsSpecified)
        {
            order.WarehouseId = request.WarehouseId.Value;
        }

        order.Lines.Clear();
        foreach (var line in request.Lines.ToEntity())
        {
            order.Lines.Add(line);
        }

        order.TotalAmount = order.Lines.Sum(line => line.TotalPrice);

        await context.SaveChangesAsync();
        await write.CommitAsync();

        return await queries.GetProjectedOrThrowAsync(order.Id);
    }

    public Task<OrderDto> ProcessAsync(ProcessOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> ShipAsync(ShipOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> RejectAsync(RejectOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> CancelAsync(CancelOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> ReturnAsync(ReturnOrderRequest request) => UpdateOrderStatus(request);

    /// <summary>
    /// Confirms delivery and promotes the order to a real Sale at the chosen warehouse: hard stock check
    /// (rule 20), a Sale on account (receivable — payment is recorded separately later), the saleId link,
    /// and the Delivered history event — all in one transaction so it can't half-apply. The order and the stock are
    /// read under the organization's write lock, so a double-clicked «Доставлен» promotes the order only once.
    /// </summary>
    public async Task<OrderDto> DeliverAsync(DeliverOrderRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        await using var write = await writeLock.BeginOrgWriteAsync();

        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId)
            ?? throw new EntityNotFoundException<Order>(request.OrderId);

        // Enforce Shipping → Delivered (throws → 409) before touching stock.
        order.ValidateTransition(DomainOrderStatus.Delivered);

        await OwnedReferences.Check()
            .Require(context.Warehouses, request.WarehouseId, nameof(request.WarehouseId))
            .ThrowIfMissingAsync();

        // Rule-20 hard block at the chosen warehouse; insufficient stock throws → rollback → 400.
        await context.MoveStockAsync(
            request.WarehouseId,
            StockMovement.StockOut,
            order.Lines.Select(l => (l.ProductId, l.Quantity, l.UnitPrice)));

        var saleLines = order.Lines.Select(ToSaleLine).ToArray();
        var sale = new TransactionRecord
        {
            PartnerId = order.CustomerId,
            Partner = null!,
            WarehouseId = request.WarehouseId,
            DateUtc = DateTimeOffset.UtcNow,
            Type = Domain.Enums.TransactionType.Sale,
            Lines = saleLines,
            TotalDue = saleLines.Sum(l => l.Total),
            TotalPaid = 0m,
            Status = Domain.Enums.TransactionStatus.Open,
        };
        sale.Number = await allocator.AllocateAsync(Ombor.Domain.Enums.NumberSeriesType.Transaction);
        context.Transactions.Add(sale);
        await context.SaveChangesAsync();

        var previous = order.Status;
        order.Status = DomainOrderStatus.Delivered;
        order.SaleId = sale.Id;
        AppendStatusEvent(order, previous, DomainOrderStatus.Delivered);
        await context.SaveChangesAsync();

        await write.CommitAsync();

        return await queries.GetProjectedOrThrowAsync(order.Id);
    }

    /// <summary>
    /// The customer, warehouse and every line's product must belong to the caller's organization (rule 34): a
    /// foreign customer would otherwise receive the Sale this order becomes on delivery.
    /// </summary>
    private Task EnsureReferencesOwnedAsync(int customerId, int? warehouseId, IEnumerable<CreateOrderLineRequest> lines) =>
        OwnedReferences.Check()
            .Require(context.Partners, customerId, nameof(CreateOrderRequest.CustomerId))
            .Require(context.Warehouses, warehouseId, nameof(CreateOrderRequest.WarehouseId))
            .Require(context.Products, lines.Select((l, i) => (l.ProductId, $"Lines[{i}].ProductId")))
            .ThrowIfMissingAsync();

    private static TransactionLine ToSaleLine(OrderLine line) => new()
    {
        ProductId = line.ProductId,
        UnitPrice = line.UnitPrice,
        Discount = line.Discount ?? 0m,
        DiscountType = line.DiscountType,
        Quantity = line.Quantity,
        Product = null!,
        Transaction = null!,
    };

    // Generic so the concrete request type flows to the validator — IRequestValidator resolves
    // IValidator<TRequest> by the static type, and no IValidator<IOrderStateUpdateRequest> is registered.
    private async Task<OrderDto> UpdateOrderStatus<TRequest>(TRequest request)
        where TRequest : IOrderStateUpdateRequest
    {
        await validator.ValidateAndThrowAsync(request);

        // A transition racing a delivery must see the delivered order, or a cancel could overwrite Delivered while the
        // sale it produced stays on the books.
        await using var write = await writeLock.BeginOrgWriteAsync();

        var order = await context.Orders.FirstOrDefaultAsync(x => x.Id == request.OrderId)
            ?? throw new EntityNotFoundException<Order>(request.OrderId);
        var targetStatus = request.TargetStatus.ToDomainStatus();

        if (order.Status != targetStatus)
        {
            order.ValidateTransition(targetStatus);
            AppendStatusEvent(order, order.Status, targetStatus);
            order.Status = targetStatus;

            await context.SaveChangesAsync();
        }

        await write.CommitAsync();

        return await queries.GetProjectedOrThrowAsync(order.Id);
    }

    private void AppendStatusEvent(Order order, DomainOrderStatus from, DomainOrderStatus to)
        => order.History.Add(new OrderStatusEvent
        {
            At = DateTimeOffset.UtcNow,
            From = from,
            To = to,
            By = currentUser.UserId,
            Order = order,
        });
}
