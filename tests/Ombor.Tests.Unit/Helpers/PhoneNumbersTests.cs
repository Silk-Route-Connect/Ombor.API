using Ombor.Application.Helpers;

namespace Ombor.Tests.Unit.Helpers;

public sealed class PhoneNumbersTests
{
    [Theory]
    [InlineData("+998901234567", "+998901234567")]
    [InlineData("998901234567", "+998901234567")]
    [InlineData("901234567", "+998901234567")]
    [InlineData("90 123 45 67", "+998901234567")]
    [InlineData("+998 (90) 123-45-67", "+998901234567")]
    [InlineData("  +998 90 123 45 67  ", "+998901234567")]
    [InlineData("(90) 123-45-67", "+998901234567")]
    [InlineData("998123456", "+998998123456")] // 9 digits are always national, even when they start with 998
    public void TryNormalize_ReturnsCanonicalForm(string input, string expected)
    {
        Assert.True(PhoneNumbers.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-phone")]
    [InlineData("+9989012345")]       // too short
    [InlineData("+99890123456789")]   // too long
    [InlineData("+7 901 234 56 78")]  // 11 digits, foreign
    [InlineData("997901234567")]      // 12 digits without the 998 country code
    [InlineData("90.123.45.67")]      // unsupported separator
    [InlineData("90123456a")]         // letters
    [InlineData("++998901234567")]
    public void TryNormalize_RejectsAnythingElse(string? input)
    {
        Assert.False(PhoneNumbers.TryNormalize(input, out _));
        Assert.False(PhoneNumbers.IsValid(input));
    }

    [Fact]
    public void TryNormalize_RejectsOversizedInput()
    {
        Assert.False(PhoneNumbers.TryNormalize(new string('9', 10_000), out _));
    }

    [Fact]
    public void Canonical_Throws_ForUnvalidatedInput()
    {
        Assert.Throws<InvalidOperationException>(() => PhoneNumbers.Canonical("abc"));
    }
}
