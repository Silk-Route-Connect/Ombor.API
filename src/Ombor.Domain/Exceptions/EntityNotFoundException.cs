using Ombor.Domain.Common;

namespace Ombor.Domain.Exceptions;

public abstract class EntityNotFoundException : Exception
{
    public string EntityType { get; }
    public string ExceptionType { get; }
    public object Id { get; }

    protected EntityNotFoundException(Type entityType, object id)
        : base($"{entityType.Name} with ID {id} was not found.")
    {
        EntityType = entityType.Name;
        ExceptionType = nameof(EntityNotFoundException);
        Id = id;
    }
}

public sealed class EntityNotFoundException<TEntity> : EntityNotFoundException
    where TEntity : EntityBase
{
    public EntityNotFoundException(object id)
        : base(typeof(TEntity), id)
    {
    }
}
