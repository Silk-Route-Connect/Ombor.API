using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Ombor.Application.Interfaces;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.API.Extensions;

public static class StartupExtensions
{
    public static async Task<IApplicationBuilder> UseDatabaseSeederAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seederFactory = scope.ServiceProvider.GetRequiredService<IDatabaseSeederFactory>();
        var seeder = seederFactory.CreateSeeder();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        await seeder.SeedDatabaseAsync(context);

        return app;
    }

    public static IApplicationBuilder UseStaticFiles(this WebApplication app)
    {
        // Handle null WebRootPath - use ContentRoot as fallback
        var rootPath = app.Environment.WebRootPath ?? app.Environment.ContentRootPath;

        if (string.IsNullOrEmpty(rootPath))
        {
            // If both are null, use the current directory
            rootPath = Directory.GetCurrentDirectory();
        }

        var fullPath = Path.Combine(rootPath, "wwwroot", "uploads", "products");

        Directory.CreateDirectory(fullPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(fullPath),
            RequestPath = "/images/products",
        });

        return app;
    }
}
