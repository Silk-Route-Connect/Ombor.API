using System.Text.Json;
using Ombor.Contracts.Serialization;

namespace Ombor.Tests.Unit.Serialization;

public sealed class ValidatingStringEnumConverterTests
{
    private enum Sample
    {
        First,
        Second,
    }

    private sealed record Holder(Sample Value);

    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new ValidatingStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Deserialize_InvalidEnumString_ThrowsInvalidEnumValueException()
    {
        // The whole design hinges on this: a non-JsonException thrown by the converter must propagate out of
        // System.Text.Json unwrapped (not re-wrapped as a JsonException, which the input formatter would swallow).
        var exception = Assert.Throws<InvalidEnumValueException>(
            () => JsonSerializer.Deserialize<Holder>("""{"value":"Nope"}""", Options));

        Assert.Equal(nameof(Sample), exception.EnumTypeName);
        Assert.Equal("Nope", exception.AttemptedValue);
    }

    [Fact]
    public void Deserialize_ValidEnumString_RoundTrips()
    {
        var holder = JsonSerializer.Deserialize<Holder>("""{"value":"Second"}""", Options);

        Assert.Equal(Sample.Second, holder!.Value);
    }

    [Fact]
    public void Serialize_WritesEnumName()
    {
        var json = JsonSerializer.Serialize(new Holder(Sample.Second), Options);

        Assert.Contains("\"Second\"", json);
    }
}
