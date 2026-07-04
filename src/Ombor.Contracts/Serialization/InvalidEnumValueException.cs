namespace Ombor.Contracts.Serialization;

/// <summary>
/// Thrown by <see cref="ValidatingStringEnumConverter"/> when a JSON body carries a value that is not a
/// member of the target enum. It is a distinct type (not a <see cref="System.Text.Json.JsonException"/>) so
/// it propagates out of the input formatter — which swallows only malformed-input JSON exceptions — and can be
/// mapped to a 400 by a dedicated exception handler.
/// </summary>
public sealed class InvalidEnumValueException(Type enumType, string? attemptedValue)
    : Exception(BuildMessage(enumType, attemptedValue))
{
    /// <summary>Simple name of the enum type that failed to bind (e.g. <c>OrderSource</c>).</summary>
    public string EnumTypeName { get; } = enumType.Name;

    /// <summary>The offending value as it appeared in the request, or <c>null</c> when it was not a string token.</summary>
    public string? AttemptedValue { get; } = attemptedValue;

    private static string BuildMessage(Type enumType, string? attemptedValue)
        => attemptedValue is null
            ? $"The value is not a valid {enumType.Name}."
            : $"'{attemptedValue}' is not a valid {enumType.Name}.";
}
