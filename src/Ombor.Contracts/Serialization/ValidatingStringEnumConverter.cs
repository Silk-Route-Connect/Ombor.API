using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ombor.Contracts.Serialization;

/// <summary>
/// A drop-in replacement for <see cref="JsonStringEnumConverter"/> that serializes enums as strings exactly as
/// the built-in converter does, but on a failed read throws an <see cref="InvalidEnumValueException"/> instead of
/// a <see cref="JsonException"/>. The built-in converter's <see cref="JsonException"/> is swallowed by the MVC
/// input formatter into model state, so an invalid enum value reaches the action as a null argument and 500s;
/// throwing a distinct exception lets it surface as a 400 instead.
/// </summary>
/// <remarks>
/// Usable both as the globally-registered enum converter and as a <c>[JsonConverter(typeof(...))]</c> attribute on
/// individual enum types (a type-level attribute overrides the global converter, so attributed enums must point
/// here explicitly to get the same behavior).
/// </remarks>
public sealed class ValidatingStringEnumConverter : JsonConverterFactory
{
    private static readonly JsonStringEnumConverter Inner = new();

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerConverter = Inner.CreateConverter(typeToConvert, options);
        var wrapperType = typeof(ValidatingEnumConverter<>).MakeGenericType(typeToConvert);

        return (JsonConverter)Activator.CreateInstance(wrapperType, innerConverter)!;
    }

    private sealed class ValidatingEnumConverter<TEnum>(JsonConverter<TEnum> inner)
        : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Capture the raw string before delegating: on a String token GetString() does not advance the reader,
            // so the inner converter still sees the same token, and we keep the offending value for the 400 message.
            var rawValue = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

            try
            {
                return inner.Read(ref reader, typeToConvert, options);
            }
            catch (JsonException)
            {
                throw new InvalidEnumValueException(typeToConvert, rawValue);
            }
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            => inner.Write(writer, value, options);
    }
}
