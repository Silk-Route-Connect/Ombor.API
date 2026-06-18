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

        // Seeding runs outside any HTTP request, so there is no tenant on the JWT.
        // The seeder creates the tenants and pins each one (via this accessor, which
        // shares the context's scope) before stamping that tenant's seeded rows.
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor>();

        await seeder.SeedDatabaseAsync(context, tenantAccessor);

        return app;
    }

    public static IApplicationBuilder UseStaticFiles(this WebApplication app)
    {
        var webRootPath = app.Environment.WebRootPath;

        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        }

        var fullPath = Path.Combine(webRootPath, "uploads", "products");

        Directory.CreateDirectory(fullPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(fullPath),
            RequestPath = "/images/products",
        });

        return app;
    }
}
