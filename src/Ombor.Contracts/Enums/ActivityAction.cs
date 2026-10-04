using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>What happened to one record in an Activity Log operation.</summary>
[JsonConverter(typeof(Serialization.ValidatingStringEnumConverter))]
public enum ActivityAction
{
    /// <summary>The record was created.</summary>
    Created = 1,

    /// <summary>The record was edited (for a document: its settlement changed).</summary>
    Updated = 2,

    /// <summary>The record was deleted (only unreferenced master data can be).</summary>
    Deleted = 3,

    /// <summary>The record was archived.</summary>
    Archived = 4,

    /// <summary>The record was restored from the archive.</summary>
    Restored = 5,
}
