using Ombor.Application.Localization;

namespace Ombor.Tests.Unit.Localization;

public sealed class SmsTextsTests
{
    // The GSM 03.38 basic set: a message made only of these is billed per 160 characters, anything else per 70.
    private const string Gsm7 =
        "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";

    private const string Code = "1234";

    // Exactly the texts submitted to Eskiz moderation on 2026-10-06 (the approved form carries the sample code 1234).
    // A failing assertion here means the SMS would be rejected until the new text is approved.
    [Theory]
    [InlineData("uz-Latn", "Ombor: ro'yxatdan o'tish kodi 1234. Kod 5 daqiqa amal qiladi. Uni hech kimga aytmang, Ombor xodimlari kodni so'ramaydi.")]
    [InlineData("uz-Cyrl", "Ombor: ro'yxatdan o'tish kodi 1234. Kod 5 daqiqa amal qiladi. Uni hech kimga aytmang, Ombor xodimlari kodni so'ramaydi.")]
    [InlineData("ru", "Ombor: код регистрации 1234. Действует 5 минут. Никому не сообщайте.")]
    public void Registration_ShouldMatchTheApprovedText(string language, string expected) =>
        Assert.Equal(expected, SmsTexts.Registration(Code, language));

    [Theory]
    [InlineData("uz-Latn", "Ombor: yangi parol uchun kod 1234. Kod 5 daqiqa amal qiladi. Uni hech kimga aytmang, Ombor xodimlari kodni so'ramaydi.")]
    [InlineData("uz-Cyrl", "Ombor: yangi parol uchun kod 1234. Kod 5 daqiqa amal qiladi. Uni hech kimga aytmang, Ombor xodimlari kodni so'ramaydi.")]
    [InlineData("ru", "Ombor: код смены пароля 1234. Действует 5 минут. Никому не сообщайте.")]
    public void PasswordReset_ShouldMatchTheApprovedText(string language, string expected) =>
        Assert.Equal(expected, SmsTexts.PasswordReset(Code, language));

    [Theory]
    [MemberData(nameof(EveryText))]
    public void EveryText_ShouldBeOneSms(string text)
    {
        var isGsm7 = text.All(c => Gsm7.Contains(c));

        Assert.True(text.Length <= (isGsm7 ? 160 : 70), $"{text.Length} characters ({(isGsm7 ? "GSM-7" : "UCS-2")}): {text}");
    }

    [Theory]
    [MemberData(nameof(EveryText))]
    public void LatinTexts_ShouldStayInGsm7(string text)
    {
        // A Latin text with one non-GSM-7 character (‘ ’ « — …) silently costs two or three SMS instead of one.
        if (text.Any(c => c is >= 'а' and <= 'я' or >= 'А' and <= 'Я'))
        {
            return;
        }

        Assert.All(text, c => Assert.True(Gsm7.Contains(c), $"'{c}' (U+{(int)c:X4}) is not GSM-7"));
    }

    public static TheoryData<string> EveryText()
    {
        var data = new TheoryData<string>();
        var texts = SupportedLanguages.All
            .SelectMany(language => new[] { SmsTexts.Registration(Code, language), SmsTexts.PasswordReset(Code, language) })
            .Distinct();

        foreach (var text in texts)
        {
            data.Add(text);
        }

        return data;
    }
}
