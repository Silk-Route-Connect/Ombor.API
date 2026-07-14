using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

public sealed class AuthErrorShapeTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    [Fact]
    public async Task Login_ReturnsUnauthorized_ForAWrongPassword()
    {
        // A bad credential is a 401 client error, not a 500 — routed through UnauthorizedAccessExceptionHandler.
        var (_, phone) = await SeedUserAsync("OldPassw0rd");

        await _client.PostAsync<ProblemDetails>(
            "auth/login", new { phoneNumber = phone, password = "WrongPassw0rd" }, HttpStatusCode.Unauthorized);
    }
}
