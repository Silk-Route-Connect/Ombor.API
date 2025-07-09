using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Infrastructure.Persistence;
using Ombor.Infrastructure.Persistence.Interceptors;
using Ombor.Tests.Integration.Helpers.ResponseValidators;

namespace Ombor.Tests.Integration.Helpers;

public class TestingWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _databaseFixture;
    private FileSettings? _fileSettingsCache;

    private ResponseValidator? _responseValidator;
    public ResponseValidator ResponseValidator
    {
        get
        {
            // Lazily resolve FileSettings once, then reuse
            if (_fileSettingsCache is null)
            {
                var sp = Services;
                var options = sp.GetRequiredService<IOptions<FileSettings>>();
                _fileSettingsCache = options.Value;
            }

            return _responseValidator ??= new ResponseValidator(_databaseFixture.Context, _fileSettingsCache, TempWebRoot);
        }
    }

    public IApplicationDbContext Context => _databaseFixture.Context;
    public string TempWebRoot { get; }

    public TestingWebApplicationFactory(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;

        TempWebRoot = Path.Combine(Path.GetTempPath(), "test_wwwroot");
        Directory.CreateDirectory(TempWebRoot);

        Environment.SetEnvironmentVariable("ASPNETCORE_WEBROOT", TempWebRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var context = services.SingleOrDefault(service => service.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (context is not null)
            {
                services.Remove(context);
            }

            services.AddDbContext<IApplicationDbContext, ApplicationDbContext>(
                (sp, options) => options.LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .UseSqlServer(_databaseFixture.DatabaseConnectionString)
                .AddInterceptors(sp.GetRequiredService<LedgerEntryInterceptor>()));
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (Directory.Exists(TempWebRoot))
            {
                Directory.Delete(TempWebRoot, recursive: true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting temporary web root directory: {ex.Message}");
        }
    }
}
