namespace Ombor.Application.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
