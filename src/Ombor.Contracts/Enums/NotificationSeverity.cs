namespace Ombor.Contracts.Enums;

/// <summary>How urgent a notification is.</summary>
public enum NotificationSeverity
{
    /// <summary>For today's plan (orders to deliver today).</summary>
    Info,

    /// <summary>Needs attention: money overdue, a delivery late, stock running out.</summary>
    Warning,
}
