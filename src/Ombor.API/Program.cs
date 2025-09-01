using Ombor.API.Extensions;
using Ombor.Application.Extensions;
using Ombor.Infrastructure.Extensions;
using Ombor.TestDataGenerator.Extensions;

var builder = WebApplication.CreateBuilder(args);


if (builder.Environment.IsProduction())
{
    builder.WebHost.UseSentry();
}

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

    app.UseRouting();

    app.UseCors(Ombor.API.Extensions.DependencyInjection.CorsPolicyName);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.UseStaticFiles();

    await app.UseDatabaseSeederAsync();

    SentrySdk.CaptureMessage("API started...");

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