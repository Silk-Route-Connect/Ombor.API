using FluentValidation;
using FluentValidation.Results;
using Ombor.Application.Interfaces;

namespace Ombor.Application.Services.Reports;

/// <summary>
/// A report period: local (Tashkent) calendar days <see cref="From"/>–<see cref="To"/> inclusive, and the UTC instants
/// [<see cref="StartUtc"/>, <see cref="EndUtc"/>) that bound the events dated in it — the UTC day starts at 05:00 local,
/// so a period is never cut on UTC midnight.
/// </summary>
internal sealed record ReportRange(DateOnly From, DateOnly To, DateTimeOffset StartUtc, DateTimeOffset EndUtc)
{
    /// <summary>The longest period a report covers: three years keeps a by-day series and the line scan bounded.</summary>
    public const int MaxDays = 3 * 366;

    /// <summary>
    /// Applies the defaults — <c>to</c> = today, <c>from</c> = the first day of <c>to</c>'s month («этот месяц») — and
    /// rejects a reversed or over-long period with a 400 on the field to correct.
    /// </summary>
    public static ReportRange Resolve(DateOnly? from, DateOnly? to, IBusinessClock clock)
    {
        var last = to ?? clock.Today;
        var first = from ?? new DateOnly(last.Year, last.Month, 1);

        if (first > last)
        {
            throw new ValidationException([new ValidationFailure("From", "'From' must be on or before 'to'.")]);
        }

        if (last.DayNumber - first.DayNumber + 1 > MaxDays)
        {
            throw new ValidationException([new ValidationFailure("To", $"A report covers at most {MaxDays} days.")]);
        }

        return new ReportRange(first, last, clock.StartOfDay(first), clock.StartOfDay(last.AddDays(1)));
    }

    /// <summary>Every local day of the period, oldest first.</summary>
    public IEnumerable<DateOnly> Days =>
        Enumerable.Range(0, To.DayNumber - From.DayNumber + 1).Select(From.AddDays);
}
