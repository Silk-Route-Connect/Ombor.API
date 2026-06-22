using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Debt;

namespace Ombor.Application.Services;

internal sealed class DebtService(IApplicationDbContext context) : IDebtService
{
    public async Task<DebtDto[]> GetDebtsAsync()
    {
        // Unpaid/partially-paid only. Org scoping is automatic via the global query filter.
        var rows = await context.Transactions
            .AsNoTracking()
            .Where(t => t.TotalDue > t.TotalPaid)
            .Select(t => new
            {
                t.Id,
                t.Type,
                t.DateUtc,
                t.DueDate,
                t.TotalDue,
                t.TotalPaid,
                t.PartnerId,
                PartnerName = t.Partner.Name,
                t.Partner.CompanyName,
                PartnerType = t.Partner.Type,
            })
            .ToArrayAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return [.. rows
            .Select(r => new DebtDto(
                r.Id,
                r.Type.ToProvisionalNumber(r.Id),
                r.Type.ToDebtDirection(),
                r.Type.ToString(),
                r.PartnerId,
                r.PartnerName,
                r.CompanyName,
                r.PartnerType.ToString(),
                r.DateUtc,
                r.DueDate,
                r.TotalDue,
                r.TotalPaid,
                r.TotalDue - r.TotalPaid,
                AgeDays(today, r.DateUtc),
                OverdueDays(today, r.DueDate)))
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.TransactionId)];
    }

    private static int AgeDays(DateOnly today, DateTimeOffset date) =>
        Math.Max(0, today.DayNumber - DateOnly.FromDateTime(date.UtcDateTime).DayNumber);

    private static int OverdueDays(DateOnly today, DateOnly? dueDate) =>
        dueDate is { } due ? Math.Max(0, today.DayNumber - due.DayNumber) : 0;
}
