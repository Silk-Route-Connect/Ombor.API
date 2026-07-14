using Ombor.Application.Validators;

namespace Ombor.Tests.Unit.Validators;

public sealed class ValidationHelpersTests
{
    [Theory]
    // Accepted: +998 country code + 9 national digits, any operator, any formatting.
    [InlineData("+998-90-123-45-67")]
    [InlineData("+998911101212")]
    [InlineData("+99890-100-00-00")]
    [InlineData("+998711234567")]   // landline (71), previously rejected by the mobile-only rule
    [InlineData("998812345678")]    // no plus, still 998 + 9 national
    [InlineData("901234567")]       // national number only
    public void IsValidPhoneNumber_ReturnsTrue_ForUzbekNumbers(string phoneNumber)
        => Assert.True(ValidationHelpers.IsValidPhoneNumber(phoneNumber));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("asdasd")]
    [InlineData("++654++321")]       // only 6 digits
    [InlineData("qwerty123")]        // only 3 digits
    [InlineData("12345")]            // too short
    [InlineData("+998901234567890")] // too long
    public void IsValidPhoneNumber_ReturnsFalse_ForInvalidNumbers(string phoneNumber)
        => Assert.False(ValidationHelpers.IsValidPhoneNumber(phoneNumber));
}
