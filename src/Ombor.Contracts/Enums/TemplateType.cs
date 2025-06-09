using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// Enumeration for supported types of the Template.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TemplateType
{
    /// <summary>Template for Sale.</summary>
    Sale = 1,

    /// <summary>Template for Supply. </summary>
    Supply = 2,
}
