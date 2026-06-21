namespace Ombor.Contracts.Enums;

/// <summary>
/// The window that drives the dashboard's revenue KPI and time-series. Debt figures are a current
/// snapshot and ignore this (complexity notes §K).
/// </summary>
public enum DashboardPeriod
{
    Today,
    Week,
    Month,
}
