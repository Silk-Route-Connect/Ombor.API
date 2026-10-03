using System.Net;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.Auth;
using Ombor.Contracts.Responses.User;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

public sealed class AuthLoginSecurityTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    private const string Password = "Passw0rd!";
    private const string WrongPassword = "WrongPassw0rd";

    [Fact]
    public async Task Login_UnknownPhone_AndWrongPassword_ReturnTheIdentical401()
    {
        var (_, phone) = await SeedUserAsync(Password);

        var wrongPassword = await PostRawAsync("auth/login", new { phoneNumber = phone, password = WrongPassword });
        var unknownPhone = await PostRawAsync("auth/login", new { phoneNumber = NewPhone(), password = WrongPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.Status);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownPhone.Status);
        Assert.Equal(ErrorCodes.InvalidCredentials, wrongPassword.Code);
        Assert.Equal(wrongPassword.Text, unknownPhone.Text); // byte-identical: nothing tells the two apart
    }

    [Fact]
    public async Task Login_AcceptsCommonFormattings_OfTheStoredPhone()
    {
        var (_, phone) = await SeedUserAsync(Password);
        var national = phone[4..];

        string[] variants =
        [
            national,
            $"998{national}",
            $"{national[..2]} {national[2..5]} {national[5..7]} {national[7..]}",
            $"+998 ({national[..2]}) {national[2..5]}-{national[5..7]}-{national[7..]}",
        ];

        foreach (var variant in variants)
        {
            var login = await _client.PostAsync<LoginResponse>(
                "auth/login", new { phoneNumber = variant, password = Password }, HttpStatusCode.OK);

            Assert.False(string.IsNullOrEmpty(login.AccessToken));
        }
    }

    [Fact]
    public async Task Login_LocksThePhone_AfterTenFailures_EvenForTheRightPassword()
    {
        var (_, phone) = await SeedUserAsync(Password);

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            var failed = await PostRawAsync("auth/login", new { phoneNumber = phone, password = WrongPassword });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.Status);
        }

        var locked = await PostRawAsync("auth/login", new { phoneNumber = phone, password = Password });

        Assert.Equal(HttpStatusCode.TooManyRequests, locked.Status);
        Assert.Equal(ErrorCodes.RateLimited, locked.Code);
        Assert.NotNull(locked.RetryAfter);
        Assert.InRange((int)locked.Body["params"]!["retryAfterSeconds"]!, 1, 15 * 60);
    }

    [Fact]
    public async Task Login_DeactivatedAccount_IsReportedOnlyAfterTheRightPassword()
    {
        var (userId, phone) = await SeedUserAsync(Password);
        await DeactivateAsync(userId);

        var wrong = await PostRawAsync("auth/login", new { phoneNumber = phone, password = WrongPassword });
        var right = await PostRawAsync("auth/login", new { phoneNumber = phone, password = Password });

        Assert.Equal(ErrorCodes.InvalidCredentials, wrong.Code);
        Assert.Equal(HttpStatusCode.Unauthorized, right.Status);
        Assert.Equal(ErrorCodes.AccountDeactivated, right.Code);
    }

    [Fact]
    public async Task Refresh_OfADeactivatedUser_Is401_AndRevokesTheirSessions()
    {
        var (userId, phone) = await SeedUserAsync(Password);
        var login = await _client.PostAsync<LoginResponse>(
            "auth/login", new { phoneNumber = phone, password = Password }, HttpStatusCode.OK);

        await DeactivateAsync(userId);

        var refresh = await PostRawAsync("auth/refresh-token", new { refreshToken = login.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.Status);
        Assert.Equal(ErrorCodes.AccountDeactivated, refresh.Code);
        Assert.False(await _context.RefreshTokens.AsNoTracking().AnyAsync(t => t.UserId == userId && !t.IsRevoked));
    }

    [Fact]
    public async Task Refresh_WithAnUnknownToken_Is401SessionExpired()
    {
        var refresh = await PostRawAsync("auth/refresh-token", new { refreshToken = $"unknown-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.Status);
        Assert.Equal(ErrorCodes.SessionExpired, refresh.Code);
    }

    [Fact]
    public async Task Login_PrunesTheUsersDeadTokens_OlderThan30Days_AndKeepsRecentOnes()
    {
        var (userId, phone) = await SeedUserAsync(Password);
        var ancient = AddToken(userId, expiresAt: DateTime.UtcNow.AddDays(-40), revoked: true);
        var expiredLongAgo = AddToken(userId, expiresAt: DateTime.UtcNow.AddDays(-1), revoked: false);
        var recentlyRotated = AddToken(userId, expiresAt: DateTime.UtcNow.AddDays(20), revoked: true);
        await _context.SaveChangesAsync();

        await _client.PostAsync<LoginResponse>("auth/login", new { phoneNumber = phone, password = Password }, HttpStatusCode.OK);

        var remaining = await _context.RefreshTokens.AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync();

        Assert.DoesNotContain(ancient.Token, remaining);
        Assert.DoesNotContain(expiredLongAgo.Token, remaining); // issued 31 days ago with the 30-day lifetime
        Assert.Contains(recentlyRotated.Token, remaining);      // issued 10 days ago: kept
        Assert.Equal(2, remaining.Count);                       // + the session just opened
    }

    private RefreshToken AddToken(int userId, DateTime expiresAt, bool revoked)
    {
        var token = new RefreshToken
        {
            Token = $"tok-{Guid.NewGuid():N}",
            ExpiresAt = expiresAt,
            IsRevoked = revoked,
            UserId = userId,
            User = null!,
        };

        _context.RefreshTokens.Add(token);

        return token;
    }

    private Task<TenantUserDto> DeactivateAsync(int userId) =>
        _client.PostAsync<TenantUserDto>($"settings/users/{userId}/deactivate", new { }, HttpStatusCode.OK);
}
