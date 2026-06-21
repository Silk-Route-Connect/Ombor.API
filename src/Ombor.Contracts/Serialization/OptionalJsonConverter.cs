using System.Text.Json;
using System.Text.Json.Serialization;
using Ombor.Contracts.Common;

namespace Ombor.Contracts.Serialization;

/// <summary>
/// Builds a converter for any <see cref="Optional{T}"/>. A present field (including an explicit
/// <c>null</c>) deserializes to a specified value; an absent field is left as the struct default
/// (<see cref="Optional{T}.IsSpecified"/> = <c>false</c>), which the framework supplies without
/// invoking the converter.
/// </summary>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(valueType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    // Optional<T> is a non-nullable struct, so the converter must run for an explicit null token too
    // (that is how "null = clear" is distinguished from an absent field).
    public override bool HandleNull => true;

    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(JsonSerializer.Deserialize<T>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.Value, options);
}
