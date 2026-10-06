namespace Ombor.Contracts.Enums;

/// <summary>
/// How a report splits its rows. The time groupings follow the local (Tashkent) calendar — a week starts on Monday — and
/// list every bucket of the range, empty ones included; the entity groupings list only rows with activity.
/// </summary>
public enum ReportGroupBy
{
    Day,
    Week,
    Month,
    Product,
    Category,
    Partner,
    Warehouse,
}
