using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

public abstract class AuthTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => "auth";
    protected override string GetUrl(int id) => $"auth/{id}";

    // A valid Uzbek mobile: +998 followed by a 9xxxxxxxx local part (rule the phone validator enforces).
    protected static string NewPhone() =>
        $"+9989{Math.Abs(Guid.NewGuid().GetHashCode()) % 100_000_000:D8}";

    /// <summary>Seeds an active, phone-confirmed user with a known password and returns its id + phone.</summary>
    protected async Task<(int userId, string phone)> SeedUserAsync(string password)
    {
        var phone = NewPhone();

        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var hash = hasher.HashPassword(password);

        var user = new User
        {
            FirstName = "Reset",
            LastName = "User",
            PhoneNumber = phone,
            PasswordHash = hash.Hash,
            PasswordSalt = hash.Salt,
            IsPhoneNumberConfirmed = true,
            Organization = null!,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (user.Id, phone);
    }

    /// <summary>
    /// Reads the OTP the server actually issued (via the shared in-memory store), instead of assuming a fixed
    /// stub — the provider mints a random code (<c>OtpCodeProvider.GenerateOtpAsync</c>).
    /// </summary>
    protected async Task<string> GetIssuedOtpAsync(string phone, OtpPurpose purpose)
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IOtpCodeProvider>();

        var otp = await provider.GetOtpAsync(phone, purpose);
        Assert.NotNull(otp);

        return otp!.Code;
    }
}
