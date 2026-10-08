namespace Ombor.Application.Helpers;

/// <summary>
/// The one canonical form of a user's phone number: <c>+998XXXXXXXXX</c>. Every account write and lookup
/// (register, verification, login, forgot/verify/reset password, invite) goes through <see cref="TryNormalize"/>,
/// so the unique index on <c>User.PhoneNumber</c> holds on the canonical value and a login never depends on how
/// the client formatted the number.
/// </summary>
internal static class PhoneNumbers
{
    private const string CountryCode = "998";
    private const int NationalNumberLength = 9;

    // Generous bound for a formatted number; also keeps the stack buffer below small for hostile input.
    private const int MaxInputLength = 32;

    /// <summary>
    /// Accepts 9 national digits or 12 digits starting with 998, optionally with a leading <c>+</c> and spaces,
    /// dashes or parentheses between digits. Anything else (letters, other symbols, other lengths) is rejected.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(input) || input.Length > MaxInputLength)
        {
            return false;
        }

        var value = input.Trim();

        if (value.StartsWith('+'))
        {
            value = value[1..];
        }

        Span<char> digits = stackalloc char[value.Length];
        var count = 0;

        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c))
            {
                digits[count++] = c;
            }
            else if (c is not (' ' or '-' or '(' or ')'))
            {
                return false;
            }
        }

        var number = new string(digits[..count]);

        if (number.Length == NationalNumberLength)
        {
            normalized = $"+{CountryCode}{number}";
            return true;
        }

        if (number.Length == CountryCode.Length + NationalNumberLength && number.StartsWith(CountryCode, StringComparison.Ordinal))
        {
            normalized = $"+{number}";
            return true;
        }

        return false;
    }

    public static bool IsValid(string? input) => TryNormalize(input, out _);

    /// <summary>
    /// The canonical form of a number the request validator already accepted; failing here is a programming error
    /// (a path that skipped validation), not bad input.
    /// </summary>
    public static string Canonical(string input) =>
        TryNormalize(input, out var normalized)
            ? normalized
            : throw new InvalidOperationException("A phone number must be validated before it is normalized.");
}
