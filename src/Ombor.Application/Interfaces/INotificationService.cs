using Ombor.Contracts.Responses.Notification;

namespace Ombor.Application.Interfaces;

/// <summary>The organization's alerts (the topbar bell), computed when asked.</summary>
public interface INotificationService
{
    /// <summary>Every alert with something to report: overdue sales, late and today's orders, low stock.</summary>
    Task<NotificationDto[]> GetAsync();
}
