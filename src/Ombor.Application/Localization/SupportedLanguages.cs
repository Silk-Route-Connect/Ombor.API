namespace Ombor.Application.Localization;

/// <summary>
/// The interface languages the app supports. This is the single source of truth for the allowed values
/// of <c>User.Language</c> — used by the registration language header, the set-language validator, and the
/// organization-setup starter-name table. It is a fixed list, not a general server-side i18n framework.
/// </summary>
public static class SupportedLanguages
{
    public const string Russian = "ru";
    public const string UzbekLatin = "uz-Latn";
    public const string UzbekCyrillic = "uz-Cyrl";

    /// <summary>All supported language codes, in display order.</summary>
    public static readonly string[] All = [Russian, UzbekLatin, UzbekCyrillic];

    public static bool IsSupported(string? language) =>
        language is not null && Array.IndexOf(All, language) >= 0;
}
