using Ombor.API.Extensions;
using Ombor.Application.Extensions;
using Ombor.Infrastructure.Extensions;
using Ombor.TestDataGenerator.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    // Drops 4xx client errors and strips credentials from captured request bodies (SentryEventFilter).
    builder.WebHost.UseSentry(options => options.SetBeforeSend(SentryEventFilter.BeforeSend));
}

try
{
    builder.Services
        .AddApi(builder.Configuration)
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration, builder.Environment)
        .AddTestDataGenerator(builder.Configuration);

    var app = builder.Build();

    // The full API schema is not published on the production host.
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseExceptionHandler(_ => { });

    app.UseForwardedHeaders();

    app.UseRouting();

    app.UseCors(Ombor.API.Extensions.DependencyInjection.CorsPolicyName);

    // After CORS, so a 429 still carries the CORS headers the browser needs to read it.
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Anonymous liveness endpoint for the container HEALTHCHECK; bypasses the global authorize filter.
    app.MapHealthChecks("/health").AllowAnonymous();

    app.UseStaticFiles();

    await app.UseDatabaseSeederAsync();

    await app.RunAsync();
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
    throw;
}

#pragma warning disable S1118 // For API tests
public partial class Program;
#pragma warning restore S1118
