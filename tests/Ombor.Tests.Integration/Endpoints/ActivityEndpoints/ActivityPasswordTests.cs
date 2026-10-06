using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Endpoints.AuthEndpoints;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ActivityEndpoints;

/// <summary>
/// A user's password change is in the Activity Log by name only: neither the hash nor the salt is ever stored in an
/// audit row or served. Runs on the real JWT host so the change is made as that user.
/// </summary>
public sealed class ActivityPasswordTests(JwtTestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output), IClassFixture<JwtTestingWebApplicationFactory>
{
    private const string Password = "Passw0rd!";
    private const string NewPassword = "N3wPassw0rd!";

    [Fact]
    public async Task PasswordChange_IsLoggedByName_NeverWithTheSecret()
    {
        using var client = CreateRawClient();
        var (userId, phone) = await SeedUserAsync(Password);
        var login = await SendAsync(client, HttpMethod.Post, "auth/login", new { phoneNumber = phone, password = Password });
        var accessToken = (string)login.Body["accessToken"]!;

        var change = await SendAsync(
            client,
            HttpMethod.Put,
            "settings/password",
            new { currentPassword = Password, newPassword = NewPassword, confirmPassword = NewPassword },
            bearerToken: accessToken,
            refreshTokenCookie: (string)login.Body["refreshToken"]!);
        Assert.Equal(HttpStatusCode.NoContent, change.Status);

        var history = await SendAsync(client, HttpMethod.Get, $"activity?entityKind=User&entityId={userId}", bearerToken: accessToken);
        Assert.Equal(HttpStatusCode.OK, history.Status);

        var item = (JObject)history.Body["items"]![0]!;
        Assert.Equal("UserUpdated", (string)item["kind"]!);
        Assert.Equal(userId, (int)item["actor"]!["id"]!);
        var field = Assert.Single(item["changes"]![0]!["fields"]!);
        Assert.Equal("password", (string)field["field"]!);
        Assert.Null(field["old"]);
        Assert.Null(field["new"]);

        var user = await _context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.Id == userId);
        var rows = await _context.AuditEntries.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.EntityType == nameof(User) && a.EntityId == userId)
            .ToListAsync();

        Assert.NotEmpty(rows);
        Assert.All(rows, row =>
        {
            var stored = $"{row.OldValues}{row.NewValues}";
            Assert.DoesNotContain(user.PasswordHash, stored);
            Assert.DoesNotContain(user.PasswordSalt, stored);
            Assert.DoesNotContain(nameof(User.PasswordHash), stored);
            Assert.DoesNotContain(nameof(User.PasswordSalt), stored);
        });
        Assert.DoesNotContain(user.PasswordHash, history.Text);
        Assert.DoesNotContain(user.PasswordSalt, history.Text);
    }
}
