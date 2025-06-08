using System.Text.RegularExpressions;

namespace Ombor.Application.Validators;
internal static class ValidationHelpers
{
    public static bool IsValidPhoneNumber(string phoneNumber) => Regex.IsMatch(phoneNumber, @"^\+998(9[0-9]|8[8])\d{7}$");
}
