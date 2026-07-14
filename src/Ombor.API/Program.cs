using FluentValidation;
using Ombor.API.Extensions;
using Ombor.Application.Extensions;
using Ombor.Domain.Exceptions;
using Ombor.Infrastructure.Extensions;
using Ombor.TestDataGenerator.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    builder.WebHost.UseSentry(options =>
    {
        // 4xx / auth failures are client errors, not server faults — keep them out of the Sentry issue stream.
        options.SetBeforeSend(static (SentryEvent @event, SentryHint _) =>
            @event.Exception is ValidationException or EntityNotFoundException or UnauthorizedAccessException
                ? null
                : @event);
    });
}

try
{
    builder.Services
        .AddApi(builder.Configuration)
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration)
        .AddTestDataGenerator(builder.Configuration);

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseExceptionHandler(_ => { });

    app.UseForwardedHeaders();

    app.UseRouting();

    app.UseCors(Ombor.API.Extensions.DependencyInjection.CorsPolicyName);

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
