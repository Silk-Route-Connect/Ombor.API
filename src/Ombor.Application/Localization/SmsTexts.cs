using Ombor.Application.Configurations;

namespace Ombor.Application.Localization;

/// <summary>
/// The SMS texts the app sends, one per purpose and language. Eskiz delivers only texts approved in its cabinet
/// (СМС → Мои тексты) and rejects anything else with 400 «не прошёл модерацию», so each text here must match an
/// approved one character for character. Changing a text needs a new moderation request first; deploy only after it
/// is approved.
/// </summary>
/// <remarks>
/// Each text is one billed SMS with a 4-digit code (the length the texts were approved with). Latin texts use only
/// GSM-7 characters — a typographic apostrophe (‘) would switch the whole message to UCS-2 and cap a single SMS at
/// 70 characters instead of 160. Cyrillic is always UCS-2, so the Russian texts stay within 70. Uzbek Cyrillic users
/// get the Uzbek Latin text: no Uzbek Cyrillic text is approved, and it would cost the same as Russian.
/// </remarks>
public static class SmsTexts
{
    /// <summary>The <c>SmsMessage.Subject</c>. Neither sent to Eskiz (it shows its own sender id) nor logged.</summary>
    public const string Subject = "Ombor";

    private const int Minutes = OtpSettings.CodeLifetimeMinutes;

    /// <summary>The registration code, in the interface language the user registers in.</summary>
    public static string Registration(string code, string? language) => IsRussian(language)
        ? $"Ombor: код регистрации {code}. Действует {Minutes} минут. Никому не сообщайте."
        : $"Ombor: ro'yxatdan o'tish kodi {code}. Kod {Minutes} daqiqa amal qiladi. Uni hech kimga aytmang, Ombor xodimlari kodni so'ramaydi.";

    /// <summary>The password-reset code (also an invited user's first sign-in), in the account's language.</summary>
    public static string PasswordReset(string code, string? language) => IsRussian(language)
        ? $"Ombor: код смены пароля {code}. Действует {Minutes} минут. Никому не сообщайте."
        : $"Ombor: yangi parol uchun kod {code}. Kod {Minutes} daqiqa amal qiladi. Uni hech kimga aytmang, Ombor xodimlari kodni so'ramaydi.";

    private static bool IsRussian(string? language) => language == SupportedLanguages.Russian;
}
