namespace Ombor.Infrastructure.Persistence.DataFixes;

/// <summary>
/// Rewrites payroll periods saved as a translated month label («Июнь 2026», «Iyun 2026», «Июн 2026», «June 2026») to
/// the agreed <c>YYYY-MM</c>. The payments module once sent the label it displayed, so the stored value read in the
/// language of whoever saved it. Month names cover Russian (nominative and genitive), Uzbek Latin and Cyrillic, and
/// English, compared case-insensitively. Idempotent: a <c>YYYY-MM</c> value has no space and never matches; a value
/// it cannot read is left untouched. Kept as a constant so the migration and its test run the same statement.
/// </summary>
internal static class PayrollPeriodBackfill
{
    public const string Sql = @"
UPDATE p
SET p.[Period] = parts.[Year] + N'-' + RIGHT(N'0' + CAST(months.[Month] AS nvarchar(2)), 2)
FROM [Payment] p
CROSS APPLY (SELECT LTRIM(RTRIM(p.[Period])) AS [Value]) v
CROSS APPLY (SELECT CHARINDEX(N' ', v.[Value]) AS [Space]) s
CROSS APPLY (SELECT
    CASE WHEN s.[Space] > 1 THEN LOWER(LEFT(v.[Value], s.[Space] - 1)) END AS [MonthName],
    CASE WHEN s.[Space] > 1 THEN LTRIM(SUBSTRING(v.[Value], s.[Space] + 1, 50)) END AS [Year]) parts
JOIN (VALUES
    (N'январь', 1), (N'января', 1), (N'январ', 1), (N'yanvar', 1), (N'january', 1),
    (N'февраль', 2), (N'февраля', 2), (N'феврал', 2), (N'fevral', 2), (N'february', 2),
    (N'март', 3), (N'марта', 3), (N'mart', 3), (N'march', 3),
    (N'апрель', 4), (N'апреля', 4), (N'апрел', 4), (N'aprel', 4), (N'april', 4),
    (N'май', 5), (N'мая', 5), (N'may', 5),
    (N'июнь', 6), (N'июня', 6), (N'июн', 6), (N'iyun', 6), (N'june', 6),
    (N'июль', 7), (N'июля', 7), (N'июл', 7), (N'iyul', 7), (N'july', 7),
    (N'август', 8), (N'августа', 8), (N'avgust', 8), (N'august', 8),
    (N'сентябрь', 9), (N'сентября', 9), (N'сентябр', 9), (N'sentabr', 9), (N'sentyabr', 9), (N'september', 9),
    (N'октябрь', 10), (N'октября', 10), (N'октябр', 10), (N'oktabr', 10), (N'oktyabr', 10), (N'october', 10),
    (N'ноябрь', 11), (N'ноября', 11), (N'ноябр', 11), (N'noyabr', 11), (N'november', 11),
    (N'декабрь', 12), (N'декабря', 12), (N'декабр', 12), (N'dekabr', 12), (N'december', 12)
) months([Name], [Month]) ON months.[Name] = parts.[MonthName]
WHERE p.[Period] IS NOT NULL
  AND parts.[Year] LIKE N'[12][0-9][0-9][0-9]';";
}
