namespace Ombor.Domain.Exceptions;

public sealed class EnumParseException<TSource, TDestination> : Exception
    where TSource : Enum
    where TDestination : Enum
{
    public string SourceType { get; }
    public string DestinationType { get; }

    public EnumParseException() : base()
    {
        SourceType = typeof(TSource).Name;
        DestinationType = typeof(TDestination).Name;
    }

    public EnumParseException(string? message) : base(message)
    {
        SourceType = typeof(TSource).Name;
        DestinationType = typeof(TDestination).Name;
    }

    public EnumParseException(string? message, Exception? innerException) : base(message, innerException)
    {
        SourceType = typeof(TSource).Name;
        DestinationType = typeof(TDestination).Name;
    }
}
