using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// An immutable record of a single order status transition — the order's history trail. One is appended
/// on every transition, plus one at creation (with <see cref="From"/> null). Audited as part of the order, so the
/// Activity Log shows the transition with its actor.
/// </summary>
public class OrderStatusEvent : EntityBase, IOrganizationScoped, IAuditableChild
{
    AuditParent IAuditableChild.AuditParent => new(typeof(Order), OrderId);

    public int OrganizationId { get; set; }

    /// <summary>When the transition happened (UTC). The audit row's own timestamp, so not repeated in it.</summary>
    [NotAudited]
    public DateTimeOffset At { get; set; }

    /// <summary>The status before the transition; null for the creation event.</summary>
    public OrderStatus? From { get; set; }

    /// <summary>The status after the transition.</summary>
    public OrderStatus To { get; set; }

    /// <summary>The user who made the transition; null outside an authenticated request (e.g. seeding). The audit actor.</summary>
    [NotAudited]
    public int? By { get; set; }

    public int OrderId { get; set; }
    public required virtual Order Order { get; set; }
}
