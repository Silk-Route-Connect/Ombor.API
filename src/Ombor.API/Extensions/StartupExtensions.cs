using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Ombor.Application.Configurations;
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

        await context.Database.MigrateAsync();

        // Seeding runs outside any HTTP request, so there is no organization on the JWT.
        // The seeder creates the organizations and pins each one (via this accessor, which
        // shares the context's scope) before stamping that organization's seeded rows.
        var organizationAccessor = scope.ServiceProvider.GetRequiredService<IOrganizationAccessor>();

        await seeder.SeedDatabaseAsync(context, organizationAccessor);

        return app;
    }

    /// <summary>
    /// Serves every upload section (product images, transaction and payment attachments, organization logos) under
    /// <c>/{PublicUrlPrefix}</c> from <c>wwwroot/{BasePath}</c> — the same URLs the upload responses already return, so
    /// stored links stay valid. Files are public by URL (no auth on static files): every stored name is a random
    /// GUID, so a URL cannot be guessed from another. <c>nosniff</c> stops a browser from running an upload as a
    /// type other than the one its (content-checked) extension declares.
    /// </summary>
    public static IApplicationBuilder UseStaticFiles(this WebApplication app)
    {
        var webRootPath = app.Environment.WebRootPath;

        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        }

        var fileSettings = app.Services.GetRequiredService<IOptions<FileSettings>>().Value;
        var uploadsPath = Path.Combine(webRootPath, fileSettings.BasePath);

        Directory.CreateDirectory(uploadsPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadsPath),
            RequestPath = $"/{fileSettings.PublicUrlPrefix}",
            OnPrepareResponse = context =>
                context.Context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff",
        });

        return app;
    }
}
