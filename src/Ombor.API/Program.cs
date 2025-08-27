using Ombor.API.Extensions;
using Ombor.Application.Extensions;
using Ombor.Infrastructure.Extensions;
using Ombor.TestDataGenerator.Extensions;

var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.UseSentry();

SentrySdk.Init(o =>
{
    o.Dsn = "https://aa99676eb90be73842484c78b9a6adc6@o4508670634557440.ingest.de.sentry.io/4509785954713680";
    o.Debug = true;
});

try
{
    SentrySdk.CaptureMessage("Starting API...");

    builder.Services
        .AddApi(builder.Configuration)
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration)
        .AddTestDataGenerator(builder.Configuration);

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseExceptionHandler(_ => { });

    app.UseHttpsRedirection();

    app.UseCors(Ombor.API.Extensions.DependencyInjection.CorsPolicyName);

    app.UseAuthorization();

    app.MapControllers();

    app.UseStaticFiles();

    await app.UseDatabaseSeederAsync();

    await app.RunAsync();
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
}

#pragma warning disable S1118 // For API tests
public partial class Program;
#pragma warning restore S1118