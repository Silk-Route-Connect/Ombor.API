namespace Ombor.Contracts.Enums;

/// <summary>
/// ISO-like codes supported by the system.
/// </summary>
public enum PaymentCurrency
{
    /// <summary>
    /// Uzbekistan soʻm — treated as the *local* currency unless user configured different.
    /// </summary>
    UZS = 0,

    /// <summary>
    /// United States dollar.
    /// </summary>
    USD = 1,

    /// <summary>
    /// Russian ruble.
    /// </summary>
    RUB = 2
}
