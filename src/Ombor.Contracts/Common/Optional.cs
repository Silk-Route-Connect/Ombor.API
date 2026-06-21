using System.Text.Json.Serialization;
using Ombor.Contracts.Serialization;

namespace Ombor.Contracts.Common;

/// <summary>
/// A value that distinguishes "not supplied" from "supplied (possibly null)". Used for PATCH-style
/// tri-state request fields where <c>undefined</c> = keep, <c>null</c> = clear, and a value = set.
/// A field left out of the JSON deserializes to <see cref="IsSpecified"/> = <c>false</c>.
/// </summary>
/// <typeparam name="T">The wrapped value type.</typeparam>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    /// <summary>Whether the field was present in the request at all.</summary>
    public bool IsSpecified { get; }

    /// <summary>The supplied value (meaningful only when <see cref="IsSpecified"/> is <c>true</c>).</summary>
    public T Value { get; }

    /// <summary>Creates a specified value.</summary>
    public Optional(T value)
    {
        Value = value;
        IsSpecified = true;
    }
}
