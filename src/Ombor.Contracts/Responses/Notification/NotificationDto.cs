using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Notification;

/// <summary>
/// One alert for the organization, computed when asked (nothing is stored or marked read): how many records it
/// concerns, their total where money is involved, and the most pressing of them.
/// </summary>
/// <param name="Kind">What the alert is about.</param>
/// <param name="Severity">How urgent it is.</param>
/// <param name="Count">How many records it concerns (always more than 0 — an alert with nothing to report is not served).</param>
/// <param name="Amount">The money involved: the remaining amount of the overdue sales; null for the other kinds.</param>
/// <param name="Items">Up to 10 of the records, most pressing first.</param>
public sealed record NotificationDto(
    NotificationKind Kind,
    NotificationSeverity Severity,
    int Count,
    decimal? Amount,
    NotificationItemDto[] Items);

/// <summary>A record an alert concerns.</summary>
/// <param name="EntityKind">The record kind (Product, Sale or Order) — the Activity Log's vocabulary, which names the page that opens it.</param>
/// <param name="Id">The record id.</param>
/// <param name="Label">The product name, or the document number (bare); null for a document without a number (old seed rows).</param>
/// <param name="Detail">The product's SKU, or the partner of a sale / the customer of an order.</param>
/// <param name="Amount">A sale's remaining amount, an order's total.</param>
/// <param name="Quantity">Low stock: the product's stock over all warehouses.</param>
/// <param name="Threshold">Low stock: the product's threshold.</param>
/// <param name="Measurement">Low stock: the product's unit (UnitOfMeasurement name).</param>
/// <param name="Date">A sale's due date, an order's delivery date.</param>
/// <param name="Days">Days past that date (0 = due today).</param>
public sealed record NotificationItemDto(
    ActivityEntityKind EntityKind,
    int Id,
    string? Label,
    string? Detail,
    decimal? Amount = null,
    decimal? Quantity = null,
    decimal? Threshold = null,
    string? Measurement = null,
    DateOnly? Date = null,
    int? Days = null);
