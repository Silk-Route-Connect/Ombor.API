using Ombor.Application.Helpers;

namespace Ombor.Tests.Unit.Helpers;

public sealed class SearchTextTests
{
    [Theory]
    [InlineData("Холматов", "Xolmatov")]
    [InlineData("Холматов", "Kholmatov")]
    [InlineData("Жасур", "Jasur")]
    [InlineData("Жасур", "Zhasur")]
    [InlineData("Ўзбекистон", "O‘zbekiston")]
    [InlineData("Ўзбекистон", "Oʻzbekiston")]
    [InlineData("Ўзбекистон", "O'zbekiston")]
    [InlineData("Ўзбекистон", "Ozbekiston")]
    [InlineData("Ғайрат", "G‘ayrat")]
    [InlineData("Шоколад", "Shokolad")]
    [InlineData("ШОКОЛАД", "shokolad")]
    [InlineData("Қўқон", "Qo‘qon")]
    [InlineData("Ҳамид", "Hamid")]
    [InlineData("Цемент", "Sement")]
    [InlineData("Цемент", "Tsement")]
    [InlineData("Чорсу  бозори", "chorsu bozori")]
    public void Skeleton_GivesBothScriptsTheSameForm(string cyrillic, string latin)
    {
        Assert.Equal(SearchText.Skeleton(cyrillic), SearchText.Skeleton(latin));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData(" Coca-Cola 0,5 ", "coca-cola 0,5")]
    [InlineData("SKU-12", "sku-12")]
    public void Skeleton_LowercasesTrimsAndKeepsOtherCharacters(string? text, string expected)
    {
        Assert.Equal(expected, SearchText.Skeleton(text));
    }

    [Theory]
    [InlineData("793", 793)]
    [InlineData("№793", 793)]
    [InlineData(" № 793 ", 793)]
    [InlineData("#12", 12)]
    [InlineData("79a", null)]
    [InlineData("Чай", null)]
    [InlineData("1234567890", null)]
    [InlineData("-5", null)]
    public void DocumentNumber_ReadsANumberWithAnOptionalSign(string query, int? expected)
    {
        Assert.Equal(expected, SearchText.DocumentNumber(query));
    }

    [Theory]
    [InlineData("90 123", true)]
    [InlineData("+998 (90) 123-45-67", true)]
    [InlineData("1234", true)]
    [InlineData("123", false)]
    [InlineData("Coca-Cola", false)]
    [InlineData("90 123 Ali", false)]
    public void IsPhoneQuery_NeedsDigitsOnlyAndEnoughOfThem(string query, bool expected)
    {
        Assert.Equal(expected, SearchText.IsPhoneQuery(query, minimumDigits: 4));
    }

    [Fact]
    public void Digits_KeepsOnlyDigits()
    {
        Assert.Equal("998901234567", SearchText.Digits("+998 (90) 123-45-67"));
        Assert.Equal(string.Empty, SearchText.Digits(null));
    }
}
