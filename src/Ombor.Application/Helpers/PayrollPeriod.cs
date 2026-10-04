using System.Text.RegularExpressions;

namespace Ombor.Application.Helpers;

/// <summary>
/// The payroll period wire format: a calendar month as <c>YYYY-MM</c> (e.g. <c>2026-06</c>). Stored and served in this
/// form only, so every client renders it in its own language — a translated label («Июнь 2026») would read in the
/// language of whoever saved it.
/// </summary>
internal static partial class PayrollPeriod
{
    public const string FormatMessage = "Period must be a month in the form YYYY-MM, e.g. 2026-06.";

    public static bool IsValid(string? period) => period is not null && Pattern().IsMatch(period);

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
