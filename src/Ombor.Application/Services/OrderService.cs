using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Abstractions;
using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class OrderService(
    IApplicationDbContext context,
    IRequestValidator validator) : IOrderService
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = request.ToEntity();

        context.Orders.Add(entity);
        await context.SaveChangesAsync();

        var createdOrder = await context.Orders
            .Include(x => x.Customer)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .FirstAsync(x => x.Id == entity.Id);

        return createdOrder.ToDto();
    }

    public async Task<OrderDto[]> GetAsync(GetOrdersRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var query = GetQuery(request);

        var result = await query.ToListAsync();

        return [.. result.Select(x => x.ToDto())];
    }

    public async Task<OrderDto> GetByIdAsync(GetOrderByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var order = await context.Orders
            .Include(x => x.Customer)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId)
            ?? throw new EntityNotFoundException<Order>(request.OrderId);

        return order.ToDto();
    }

    public async Task ProcessAsync(ProcessOrderRequest request) => await UpdateOrderStatus(request);

    public async Task ShipAsync(ShipOrderRequest request) => await UpdateOrderStatus(request);

    public async Task RejectAsync(RejectOrderRequest request) => await UpdateOrderStatus(request);

    public async Task CancelAsync(CancelOrderRequest request) => await UpdateOrderStatus(request);

    public async Task ReturnAsync(ReturnOrderRequest request) => await UpdateOrderStatus(request);

    public async Task DeliverAsync(DeliverOrderRequest request) => await UpdateOrderStatus(request);

    private async Task UpdateOrderStatus(IOrderStateUpdateRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var order = await GetOrThrowAsync(request.OrderId);

        if (order.Status == OrderStatus.Processing)
        {
            return;
        }

        var targetStatus = request.TargetStatus.ToDomainStatus();
        order.ValidateTransition(targetStatus);
        order.Status = targetStatus;

        await context.SaveChangesAsync();
    }

    private IQueryable<Order> GetQuery(GetOrdersRequest request)
    {
        var query = context.Orders
            .Include(x => x.Customer)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Notes != null && x.Notes.Contains(request.SearchTerm));
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId);
        }

        // TODO: from and to dates are expected to be local times, parse them to UTC
        if (request.FromDate.HasValue)
        {
            query = query.Where(x => x.DateUtc >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(x => x.DateUtc <= request.ToDate);
        }

        return query;
    }

    private async Task<Order> GetOrThrowAsync(int orderId, bool eagerLoad = false)
    {
        var order = eagerLoad
            ? await context.Orders
                .Include(x => x.Customer)
                .Include(x => x.Lines)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == orderId)
            : await context.Orders
                .FirstOrDefaultAsync(x => x.Id == orderId);

        return order ?? throw new EntityNotFoundException<Order>(orderId);
    }
}
