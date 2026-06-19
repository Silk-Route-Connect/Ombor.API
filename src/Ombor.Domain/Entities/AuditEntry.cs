using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Immutable audit record for a money- or stock-affecting change. Written by the audit
/// interceptor; never edited or deleted.
/// </summary>
public class AuditEntry : EntityBase, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>The entity type that changed, e.g. "TransactionRecord".</summary>
    public required string EntityType { get; set; }

    /// <summary>Primary key of the changed entity.</summary>
    public int EntityId { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>JSON snapshot of the changed columns before the change; null for inserts.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the changed columns after the change; null for deletes.</summary>
    public string? NewValues { get; set; }

    /// <summary>User who performed the change; null for system or seed operations.</summary>
    public int? UserId { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }
}
