using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ombor.Application.Helpers;

/// <summary>
/// Cyrillic↔Latin search parity (product-brief; audit i18n-5). Both the query and the searched text are reduced to
/// one Latin «skeleton» before comparing, so «Холматов», «Xolmatov» and «Kholmatov», or «Ўзбекистон» and
/// «O‘zbekiston», find each other: lowercase, every apostrophe variant dropped, Cyrillic written the Uzbek-Latin way
/// (х→x, ж→j, ц→s, ў→o, ғ→g, қ→q, ҳ→h, ш→sh, ч→ch, ё→yo, ю→yu, я→ya), then Russian-style Latin folded onto it
/// (kh→x, zh→j, ts→s). The frontend's list filters should fold the same way.
/// </summary>
internal static partial class SearchText
{
    private static readonly Dictionary<char, string> CyrillicToLatin = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "yo", ['ж'] = "j",
        ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o",
        ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u", ['ф'] = "f", ['х'] = "x", ['ц'] = "s",
        ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sh", ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu",
        ['я'] = "ya", ['ў'] = "o", ['қ'] = "q", ['ғ'] = "g", ['ҳ'] = "h",
    };

    // ASCII and typographic apostrophes, the Uzbek okina (ʻ) and modifier apostrophe (ʼ), backtick and acute accent.
    private const string Apostrophes = "'`‘’ʻʼ´";

    /// <summary>The comparable form of <paramref name="text"/>; empty for null or blank.</summary>
    public static string Skeleton(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var latin = new StringBuilder(text.Length + 8);
        var lastWasSpace = false;

        foreach (var c in text.Trim().ToLower(CultureInfo.InvariantCulture))
        {
            if (Apostrophes.Contains(c))
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    latin.Append(' ');
                }

                lastWasSpace = true;
                continue;
            }

            lastWasSpace = false;

            if (CyrillicToLatin.TryGetValue(c, out var mapped))
            {
                latin.Append(mapped);
            }
            else
            {
                latin.Append(c);
            }
        }

        return latin.Replace("kh", "x").Replace("zh", "j").Replace("ts", "s").ToString();
    }

    /// <summary>The digits of <paramref name="text"/> (a phone number however it was typed or stored).</summary>
    public static string Digits(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : new string([.. text.Where(char.IsAsciiDigit)]);

    /// <summary>
    /// Whether the query looks like a phone number: only digits, spaces, <c>+ - ( )</c>, and at least
    /// <paramref name="minimumDigits"/> digits — so «90 123» searches phones, but «Coca-Cola» or «7» do not.
    /// </summary>
    public static bool IsPhoneQuery(string query, int minimumDigits) =>
        PhoneQueryPattern().IsMatch(query) && Digits(query).Length >= minimumDigits;

    /// <summary>
    /// The document number in a query such as «793», «№793», «№ 793» or «#793»; null when the query is not a number.
    /// </summary>
    public static int? DocumentNumber(string query)
    {
        var match = DocumentNumberPattern().Match(query);

        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

    [GeneratedRegex(@"^[\d\s+\-()]+$")]
    private static partial Regex PhoneQueryPattern();

    [GeneratedRegex(@"^\s*(?:№|#)?\s*(\d{1,9})\s*$")]
    private static partial Regex DocumentNumberPattern();
}
