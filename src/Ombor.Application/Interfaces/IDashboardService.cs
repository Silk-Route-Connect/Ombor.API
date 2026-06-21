using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Dashboard;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Builds the aggregated dashboard read model. Debt figures are derived from the same source as
/// <c>GET /api/debts</c> so the two screens always agree (complexity notes §K).
/// </summary>
public interface IDashboardService
{
    /// <summary>The dashboard snapshot for the current organization; <paramref name="period"/> drives only revenue + series.</summary>
    Task<DashboardDto> GetAsync(DashboardPeriod period);
}
