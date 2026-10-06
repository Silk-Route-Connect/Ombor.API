using System.Net;
using Newtonsoft.Json.Linq;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

/// <summary>
/// Register paths that never create an account. The full register → verification flow runs in
/// <see cref="AuthSessionTests"/>, on a host without the always-signed-in test scheme, so the new account is not
/// stamped into the shared test organization.
/// </summary>
public sealed class AuthRegisterTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    [Fact]
    public async Task Register_RunsTheRequestValidator()
    {
        var response = await PostRawAsync(
            "auth/register",
            new
            {
                firstName = "",
                lastName = "",
                phoneNumber = NewPhone(),
                password = "1",
                confirmPassword = "2",
                organizationName = "",
            },
            language: "ru");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal(ErrorCodes.ValidationFailed, response.Code);

        var errors = (JObject)response.Body["errors"]!;
        Assert.All(
            new[] { "FirstName", "LastName", "Password", "ConfirmPassword", "OrganizationName" },
            field => Assert.NotNull(errors[field]));
    }

    [Fact]
    public async Task Register_MalformedPhone_IsAFieldError()
    {
        var response = await PostRawAsync("auth/register", Registration("not-a-phone"), language: "ru");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.NotNull(response.Body["errors"]!["PhoneNumber"]);
    }

    [Fact]
    public async Task Register_TakenPhone_InAnyFormatting_Is400PhoneTaken()
    {
        var (_, phone) = await SeedUserAsync("Passw0rd!");
        var national = phone[4..];

        var response = await PostRawAsync(
            "auth/register",
            Registration($"{national[..2]} {national[2..5]}-{national[5..7]}-{national[7..]}"),
            language: "ru");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal(ErrorCodes.PhoneTaken, response.Code);
        Assert.NotNull(response.Body["errors"]!["PhoneNumber"]);
    }

    [Fact]
    public async Task Register_SendsA6DigitCode_AndAnnouncesLengthAndCooldown()
    {
        var phone = NewPhone();

        var response = await PostRawAsync("auth/register", Registration(phone), language: "ru");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal(5, (int)response.Body["expiresInMinutes"]!);
        Assert.Equal(6, (int)response.Body["codeLength"]!);
        Assert.Equal(60, (int)response.Body["resendAfterSeconds"]!);
        Assert.Equal(6, (await GetIssuedOtpAsync(phone, OtpPurpose.Registration)).Length);
    }

    [Fact]
    public async Task Verification_WrongCode_Keeps400MessageShape_AndAddsTheCode()
    {
        var phone = NewPhone();
        await PostRawAsync("auth/register", Registration(phone), language: "ru");
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.Registration);

        var response = await PostRawAsync(
            "auth/verification", new { phoneNumber = phone, code = WrongCodeFor(code) }, language: "ru");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal("Invalid verification code.", (string?)response.Body["message"]);
        Assert.Equal(ErrorCodes.CodeInvalid, response.Code);
    }

    [Fact]
    public async Task Verification_WithoutAnIssuedCode_IsCodeExpired()
    {
        var response = await PostRawAsync(
            "auth/verification", new { phoneNumber = NewPhone(), code = "123456" }, language: "ru");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal(ErrorCodes.CodeExpired, response.Code);
    }

    internal static object Registration(string phone) => new
    {
        firstName = "Ali",
        lastName = "Valiyev",
        phoneNumber = phone,
        password = "Passw0rd!",
        confirmPassword = "Passw0rd!",
        organizationName = "Test Shop",
    };

    internal static string WrongCodeFor(string code) =>
        code[0] == '1' ? "2" + code[1..] : "1" + code[1..];
}
