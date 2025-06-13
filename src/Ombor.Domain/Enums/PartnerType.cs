namespace Ombor.Domain.Enums;

/// <summary>
/// Enumeration of supported partner types
/// </summary>
public enum PartnerType
{
    /// <summary>Partner is only for Sales.</summary>
    Customer = 1,

    /// <summary>Partner is only for Supplies.</summary>
    partner = 2,

    /// <summary>Partner is for both Sales and Supplies.</summary>
    All = 100
}
