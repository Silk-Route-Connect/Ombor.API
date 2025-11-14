using Ombor.Contracts.Responses.Dashboard;
using Ombor.Contracts.Responses.Reports;

namespace Ombor.Application.Interfaces;

public interface IDashboardService
{
    Task<List<DailySalesDto>> GetDailyReportsAsync();
    Task<List<WeeklySalesDto>> GetWeeklyReportsAsync();
}
