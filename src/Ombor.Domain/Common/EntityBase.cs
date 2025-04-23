namespace Ombor.Domain.Common;

/// <summary>
/// Base class for all entities, providing a primary key Id.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public int Id { get; set; }
}