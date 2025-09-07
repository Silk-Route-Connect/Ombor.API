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

    protected EntityNotFoundException() : base()
    {
        EntityType = string.Empty;
        ExceptionType = nameof(EntityNotFoundException);
        Id = string.Empty;
    }

    protected EntityNotFoundException(string? message) : base(message)
    {
        EntityType = string.Empty;
        ExceptionType = nameof(EntityNotFoundException);
        Id = string.Empty;
    }

    protected EntityNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
        EntityType = string.Empty;
        ExceptionType = nameof(EntityNotFoundException);
        Id = string.Empty;
    }
}

public class EntityNotFoundException<TEntity> : EntityNotFoundException
    where TEntity : EntityBase
{
    public EntityNotFoundException() { }

    public EntityNotFoundException(string? message) : base(message) { }

    public EntityNotFoundException(string? message, Exception? innerException) : base(message, innerException) { }

    public EntityNotFoundException(object id) : base(typeof(TEntity), id) { }

    public EntityNotFoundException(Type entityType, object id) : base(entityType, id) { }
}
