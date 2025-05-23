using Microsoft.EntityFrameworkCore;
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

        await seeder.SeedDatabaseAsync(context);

        return app;
    }
}
