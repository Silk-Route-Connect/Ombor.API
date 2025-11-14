using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Contracts.Responses.Reports;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal class DashboardService(
    IApplicationDbContext context,
    ITenantProvider tenantProvider) : IDashboardService
{
    public async Task<List<DailySalesDto>> GetDailyReportsAsync()
    {
        var today = DateTimeOffset.UtcNow;
        var startOfDay = new DateTimeOffset(today.UtcDateTime.Date, TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        var query = GetQuery();
        var transactions = query
            .Include(t => t.Partner)
            .Where(t => t.DateUtc >= startOfDay && t.DateUtc < endOfDay);

        var salesDto = await transactions
            .Select(t => new DailySalesDto
            (
                t.Id,
                t.Partner.Name,
                t.TotalDue,
                t.Lines.Count,
                t.DateUtc.DateTime))
            .ToListAsync();

        return salesDto;
    }

    public async Task<List<WeeklySalesDto>> GetWeeklyReportsAsync()
    {
        var endDate = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1), TimeSpan.Zero);
        var startDate = endDate.AddDays(-7);

        var query = GetQuery();
        var transactions = await query
            .Where(t => t.DateUtc >= startDate && t.DateUtc < endDate)
            .AsNoTracking()
            .ToListAsync();

        var weeklySales = transactions
             .GroupBy(t => t.DateUtc.Date)
             .Select(x => new WeeklySalesDto(
                 x.Count(),
                 x.Sum(t => t.TotalDue),
                 x.Key))
             .OrderBy(r => r.Date)
             .ToList();

        return weeklySales;
    }

    private IQueryable<TransactionRecord> GetQuery()
    {
        var tenantId = tenantProvider.GetCurrentTenantId();

        return context.Transactions
            .Where(t => t.Type == TransactionType.Sale && t.OrganizationId == tenantId)
            .AsNoTracking();
    }
}
