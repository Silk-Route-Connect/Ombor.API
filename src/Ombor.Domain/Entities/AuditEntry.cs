using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

/// <summary>
/// Immutable audit record of one change to an <see cref="IAuditable"/> entity (a money/stock event or master data).
/// Written by the audit interceptor; never edited or deleted. Rows of one request share an
/// <see cref="OperationId"/>, so the Activity Log shows a sale with its lines, stock and payment as one operation.
/// </summary>
public class AuditEntry : EntityBase, IOrganizationScoped
{
    public int OrganizationId { get; set; }

    /// <summary>
    /// The unit of work the change belongs to: one per HTTP request, one per save outside a request (seeding).
    /// Rows recorded before operations existed were given one id per user and second.
    /// </summary>
    public Guid? OperationId { get; set; }

    /// <summary>The entity type that changed, e.g. "TransactionRecord".</summary>
    public required string EntityType { get; set; }

    /// <summary>Primary key of the changed entity.</summary>
    public int EntityId { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>For a child row (a document line, a payment component): the parent's entity type.</summary>
    public string? ParentEntityType { get; set; }

    /// <summary>For a child row: the parent's primary key.</summary>
    public int? ParentEntityId { get; set; }

    /// <summary>JSON of the audited columns before the change (only the changed ones for an update); null for inserts.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON of the audited columns after the change (only the changed ones for an update); null for deletes.</summary>
    public string? NewValues { get; set; }

    /// <summary>User who performed the change; null for system or seed operations.</summary>
    public int? UserId { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }
}
