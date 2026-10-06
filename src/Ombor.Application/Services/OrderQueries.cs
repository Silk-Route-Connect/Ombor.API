using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Responses.Order;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

/// <summary>
/// The read side of <see cref="IOrderService"/>: the filtered list and the projected order (customer balance from
/// the org-scoped PartnerBalance view, history actor names). Split from <see cref="OrderService"/>, which keeps the
/// writes and transitions.
/// </summary>
internal sealed class OrderQueries(IApplicationDbContext context)
{
    public Task<OrderDto[]> GetAsync(GetOrdersRequest request) => ProjectAsync(GetQuery(request));

    private IQueryable<Order> GetQuery(GetOrdersRequest request)
    {
        var query = context.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm;
            // The order number is an integer now, so it matches exactly (not as a substring); notes and
            // customer name stay substring searches.
            var numberTerm = int.TryParse(term, out var parsed) ? parsed : (int?)null;
            query = query.Where(x =>
                (numberTerm != null && x.OrderNumber == numberTerm) ||
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

    public async Task<OrderDto> GetProjectedOrThrowAsync(int orderId)
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
