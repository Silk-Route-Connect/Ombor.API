namespace Ombor.Domain.Common;

/// <summary>
/// An audited row that is part of another record — a document line, a payment component, an order status event.
/// Its audit rows carry the parent, so the parent's history includes them.
/// </summary>
public interface IAuditableChild : IAuditable
{
    /// <summary>The record this row belongs to. The id may be 0 until a new parent is saved.</summary>
    AuditParent AuditParent { get; }
}

/// <summary>The parent of an <see cref="IAuditableChild"/>: its entity type and id.</summary>
/// <param name="EntityType">The parent's entity class.</param>
/// <param name="EntityId">The parent's primary key.</param>
public readonly record struct AuditParent(Type EntityType, int EntityId);
