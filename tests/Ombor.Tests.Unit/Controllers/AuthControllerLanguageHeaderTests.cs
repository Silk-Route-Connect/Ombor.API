using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ombor.API.Controllers;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Controllers;

public sealed class AuthControllerLanguageHeaderTests
{
    private const string LanguageHeader = "X-Ombor-Language";

    [Theory]
    [InlineData(null)]   // header absent
    [InlineData("")]     // header empty
    [InlineData("en")]   // unsupported language
    [InlineData("uz")]   // not one of the exact codes
    [InlineData("RU")]   // case-sensitive: not "ru"
    public async Task RegisterAsync_ShouldThrowValidation_WhenLanguageHeaderMissingOrUnsupported(string? language)
    {
        var service = new Mock<IAuthService>();
        var controller = CreateController(service.Object, language);
        var request = new RegisterRequest("F", "L", "+998900000000", "pass", "pass", "Org", null, null);

        await Assert.ThrowsAsync<ValidationException>(() => controller.RegisterAsync(request));

        // The OTP is never sent when the language header is rejected.
        service.Verify(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("uz-Latn")]
    [InlineData("uz-Cyrl")]
    public async Task RegisterAsync_ShouldForwardTheValidatedLanguage(string language)
    {
        var service = new Mock<IAuthService>();
        var controller = CreateController(service.Object, language);
        var request = new RegisterRequest("F", "L", "+998900000000", "pass", "pass", "Org", null, null);

        await controller.RegisterAsync(request);

        // The language picks the SMS text, so the one the header carried must reach the service.
        service.Verify(s => s.RegisterAsync(request, language), Times.Once);
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("uz-Latn")]
    [InlineData("uz-Cyrl")]
    public async Task SmsVerificationAsync_ShouldForwardTheValidatedLanguage(string language)
    {
        var service = new Mock<IAuthService>();
        service
            .Setup(s => s.VerifyRegistrationOtpAsync(It.IsAny<SmsVerificationRequest>(), language))
            .ReturnsAsync(new RegistrationVerification(null, ErrorCodes.CodeInvalid));
        var controller = CreateController(service.Object, language);

        await controller.SmsVerificationAsync(new SmsVerificationRequest("+998900000000", "1234"));

        service.Verify(s => s.VerifyRegistrationOtpAsync(It.IsAny<SmsVerificationRequest>(), language), Times.Once);
    }

    [Fact]
    public async Task SmsVerificationAsync_PutsTheRefreshTokenInTheCookieOnly()
    {
        var service = new Mock<IAuthService>();
        service
            .Setup(s => s.VerifyRegistrationOtpAsync(It.IsAny<SmsVerificationRequest>(), "ru"))
            .ReturnsAsync(new RegistrationVerification(new AuthSession("access-token", "refresh-token", "ru"), null));
        var controller = CreateController(service.Object, "ru");

        var result = await controller.SmsVerificationAsync(new SmsVerificationRequest("+998900000000", "1234"));

        var body = Assert.IsType<VerifyOtpResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("access-token", body.AccessToken);
        Assert.True(body.Success);
        Assert.Contains("ombor.refreshToken=refresh-token", controller.Response.Headers.SetCookie.ToString());
    }

    private static AuthController CreateController(IAuthService service, string? language)
    {
        var jwt = Options.Create(new JwtSettings
        {
            Key = new string('k', 32),
            Issuer = "issuer",
            Audience = "audience",
            AccessTokenExpiresInHours = 1,
            RefreshTokenExpiresInDays = 30,
        });
        var cookies = Options.Create(new CookieSettings { SameSite = "Lax" });

        var controller = new AuthController(
            service, Mock.Of<IPasswordResetService>(), jwt, cookies, NullLogger<AuthController>.Instance);

        var httpContext = new DefaultHttpContext();
        if (language is not null)
        {
            httpContext.Request.Headers[LanguageHeader] = language;
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }
}
