using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// An immutable record of a single order status transition — the order's history trail. One is appended
/// on every transition, plus one at creation (with <see cref="From"/> null). This log <em>is</em> the
/// order's history, so it is not separately audited.
/// </summary>
public class OrderStatusEvent : EntityBase, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>When the transition happened (UTC).</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>The status before the transition; null for the creation event.</summary>
    public OrderStatus? From { get; set; }

    /// <summary>The status after the transition.</summary>
    public OrderStatus To { get; set; }

    /// <summary>The user who made the transition; null outside an authenticated request (e.g. seeding).</summary>
    public int? By { get; set; }

    public int OrderId { get; set; }
    public required virtual Order Order { get; set; }
}
