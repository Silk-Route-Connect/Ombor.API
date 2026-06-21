using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
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
    ICurrentUserAccessor currentUser) : IOrderService
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();
        entity.History.Add(new OrderStatusEvent
        {
            At = DateTimeOffset.UtcNow,
            From = null,
            To = entity.Status,
            By = currentUser.UserId,
            Order = entity,
        });

        context.Orders.Add(entity);
        await context.SaveChangesAsync();

        return await GetProjectedOrThrowAsync(entity.Id);
    }

    public async Task<OrderDto[]> GetAsync(GetOrdersRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await ProjectAsync(GetQuery(request));
    }

    public async Task<OrderDto> GetByIdAsync(GetOrderByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        return await GetProjectedOrThrowAsync(request.OrderId);
    }

    public async Task<OrderDto> UpdateAsync(UpdateOrderRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

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
        order.DeliveryAddress = request.DeliveryAddress;
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

        return await GetProjectedOrThrowAsync(order.Id);
    }

    public Task<OrderDto> ProcessAsync(ProcessOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> ShipAsync(ShipOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> RejectAsync(RejectOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> CancelAsync(CancelOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> ReturnAsync(ReturnOrderRequest request) => UpdateOrderStatus(request);

    public Task<OrderDto> DeliverAsync(DeliverOrderRequest request) => UpdateOrderStatus(request);

    private async Task<OrderDto> UpdateOrderStatus(IOrderStateUpdateRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

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

        return await GetProjectedOrThrowAsync(order.Id);
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

    private IQueryable<Order> GetQuery(GetOrdersRequest request)
    {
        var query = context.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm;
            query = query.Where(x =>
                x.OrderNumber.Contains(term) ||
                (x.Notes != null && x.Notes.Contains(term)) ||
                x.Customer.Name.Contains(term));
        }

        if (request.Status.HasValue)
        {
            var status = request.Status.Value.ToDomainStatus();
            query = query.Where(x => x.Status == status);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.DateUtc >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.DateUtc <= request.ToDate);
        }

        return query.OrderByDescending(x => x.DateUtc).ThenByDescending(x => x.Id);
    }

    private async Task<OrderDto> GetProjectedOrThrowAsync(int orderId)
    {
        var dtos = await ProjectAsync(context.Orders.Where(x => x.Id == orderId));

        return dtos.FirstOrDefault() ?? throw new EntityNotFoundException<Order>(orderId);
    }

    private async Task<OrderDto[]> ProjectAsync(IQueryable<Order> query)
    {
        var orders = await query
            .Include(x => x.Customer)
            .Include(x => x.Warehouse)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .Include(x => x.History)
            .AsNoTracking()
            .ToListAsync();

        if (orders.Count == 0)
        {
            return [];
        }

        // customerBalance comes from the org-scoped PartnerBalance view (Total is computed in memory).
        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToArray();
        var balanceRows = await context.PartnerBalances
            .Where(b => customerIds.Contains(b.PartnerId))
            .ToArrayAsync();
        var balances = balanceRows.ToDictionary(b => b.PartnerId, b => b.Total);

        // Resolve each history actor's display name in one query.
        var actorIds = orders
            .SelectMany(o => o.History)
            .Where(h => h.By.HasValue)
            .Select(h => h.By!.Value)
            .Distinct()
            .ToArray();
        var actorNames = actorIds.Length == 0
            ? new Dictionary<int, string>()
            : await context.Users
                .Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

        return [.. orders.Select(o => o.ToDto(
            balances.TryGetValue(o.CustomerId, out var balance) ? balance : 0m,
            actorNames))];
    }
}
