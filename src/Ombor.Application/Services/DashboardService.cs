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

        var query = GetQuery();
        var transactions = query
            .Include(t => t.Partner)
            .Where(t => t.DateUtc == today);

        var salesdto = await transactions
            .Select(t => new DailySalesDto
            (
                t.Id,
                t.Partner.Name,
                t.TotalDue,
                t.Lines.Count,
                t.DateUtc.DateTime))
            .ToListAsync();

        return salesdto;
    }

    public async Task<List<WeeklySalesDto>> GetWeeklyReportsAsync()
    {
        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddDays(-6);

        var query = GetQuery();
        var transactions = await query
            .Where(t => t.DateUtc >= startDate && t.DateUtc <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var weeklySales = transactions
             .GroupBy(t => t.DateUtc.Date)
             .Select(x => new WeeklySalesDto(
                 x.Count(),
                 x.Sum(t => t.TotalDue),
                 x.Key))
             .OrderBy(r => r.DateTime)
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
