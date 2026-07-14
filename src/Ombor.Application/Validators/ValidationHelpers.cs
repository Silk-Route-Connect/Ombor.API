namespace Ombor.Application.Validators;

internal static class ValidationHelpers
{
    // Uzbek numbers are 9 national digits (e.g. 90 123 45 67); with the +998 country code, 12.
    private const int NationalNumberLength = 9;
    private const int WithCountryCodeLength = 12;

    // Policy (F): accept any Uzbek number under +998, validating length only — not the operator prefix —
    // so landlines and every mobile operator pass. Formatting characters (+, spaces, dashes) are ignored.
    // Length-based rather than prefix-stripping: a national mobile can itself start with "998".
    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        return digits.Length == NationalNumberLength
            || (digits.Length == WithCountryCodeLength && digits.StartsWith("998", StringComparison.Ordinal));
    }
}
