using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Services.Activity;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Search;
using DomainPaymentType = Ombor.Domain.Enums.PaymentType;

namespace Ombor.Application.Services.Search;

/// <summary>
/// Finds documents by exact number. Each series is numbered separately (DR-21): one transaction series shared by
/// sales, supplies and both refunds, one for payments, one for orders — so a number names at most one of each.
/// </summary>
internal sealed class SearchDocuments(IApplicationDbContext context)
{
    public async Task<SearchGroupDto> ByNumberAsync(int number, int limit)
    {
        var label = number.ToString(CultureInfo.InvariantCulture);

        var transactions = await context.Transactions
            .AsNoTracking()
            .Where(t => t.Number == number)
            .Select(t => new { t.Id, t.Type, t.DateUtc, t.TotalDue, Partner = t.Partner.Name })
            .ToArrayAsync();

        var payments = await context.Payments
            .AsNoTracking()
            .Where(p => p.Number == number)
            .Select(p => new
            {
                p.Id,
                p.Type,
                p.DateUtc,
                Amount = p.Components.Sum(c => c.Amount),
                Partner = p.Partner != null ? p.Partner.Name : null,
                Employee = p.Employee != null ? p.Employee.FullName : null,
            })
            .ToArrayAsync();

        var orders = await context.Orders
            .AsNoTracking()
            .Where(o => o.OrderNumber == number)
            .Select(o => new { o.Id, o.DateUtc, o.TotalAmount, Customer = o.Customer.Name })
            .ToArrayAsync();

        SearchHitDto[] hits =
        [
            .. transactions.Select(t => Hit(
                ActivityCatalog.KindOf(ActivityCatalog.Transaction, t.Type, null), t.Id, label, t.Partner, t.DateUtc, t.TotalDue)),
            .. payments.Select(p => Hit(
                p.Type == DomainPaymentType.Payroll ? ActivityEntityKind.Payroll : ActivityEntityKind.Payment,
                p.Id, label, p.Employee ?? p.Partner, p.DateUtc, p.Amount)),
            .. orders.Select(o => Hit(ActivityEntityKind.Order, o.Id, label, o.Customer, o.DateUtc, o.TotalAmount)),
        ];

        return new SearchGroupDto(hits.Length, [.. hits.Take(limit)]);
    }

    private static SearchHitDto Hit(
        ActivityEntityKind kind, int id, string label, string? detail, DateTimeOffset date, decimal amount) =>
        new(kind, id, label, detail, SearchMatch.Number, IsArchived: false, date, amount);
}
