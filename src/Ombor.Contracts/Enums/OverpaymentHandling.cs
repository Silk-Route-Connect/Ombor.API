using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// What to do with a payment amount that exceeds the debt it settles (business-rules rules 15, 40).
/// </summary>
[JsonConverter(typeof(Serialization.ValidatingStringEnumConverter))]
public enum OverpaymentHandling
{
    /// <summary>Hand the excess back as cash; recorded as a change-return memo, excluded from balances (rule 15).</summary>
    Change = 1,

    /// <summary>Park the excess as a partner advance; only allowed when no debt remains (rule 40).</summary>
    Advance = 2,
}
