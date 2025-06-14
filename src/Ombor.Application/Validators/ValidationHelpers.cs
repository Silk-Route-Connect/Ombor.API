using System.Text.RegularExpressions;

namespace Ombor.Application.Validators;

internal static class ValidationHelpers
{
    private const string UzPhonePattern = @"^(?:\+998-?)?(?:9\d{8}|9\d-\d{3}-\d{2}-\d{2}|9\d{2}-\d{3}-\d{3})$";

    public static bool IsValidPhoneNumber(string phoneNumber) =>
        Regex.IsMatch(phoneNumber, UzPhonePattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
}
