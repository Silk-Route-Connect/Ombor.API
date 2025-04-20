using Ombor.Domain.Common;

namespace Ombor.Domain.Exceptions;

public sealed class EntityNotFoundException<TEntity> : Exception
    where TEntity : EntityBase
{
    public string EntityType => typeof(TEntity).Name;
    public string ExceptionType => nameof(EntityNotFoundException<TEntity>);
    public object? Id { get; }

    public EntityNotFoundException()
    {
    }

    public EntityNotFoundException(object id)
        : base($"Entity of type {typeof(TEntity).Name} with ID {id} was not found.")
    {
        Id = id;
    }

    public EntityNotFoundException(string? message)
        : base(message)
    {
    }

    public EntityNotFoundException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}