using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;

namespace Ombor.Tests.Integration.Helpers;

/// <summary>
/// A test host that authenticates with the real JWT bearer scheme instead of the always-signed-in "Test" scheme,
/// for tests whose subject is the access token itself (rule-41 rejection of a deactivated user's token,
/// change-password as the signed-in user). Same database as every other host.
/// </summary>
public sealed class JwtTestingWebApplicationFactory(DatabaseFixture databaseFixture)
    : TestingWebApplicationFactory(databaseFixture)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Registered after the base "Test" defaults, so this PostConfigure wins.
        builder.ConfigureTestServices(services =>
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }));
    }
}
