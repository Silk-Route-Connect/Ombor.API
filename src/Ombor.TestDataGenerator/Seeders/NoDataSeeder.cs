using Ombor.Application.Interfaces;
using Ombor.TestDataGenerator.Interfaces;

namespace Ombor.TestDataGenerator.Seeders;

/// <summary>
/// The Production and Staging seeder: seeds nothing. Real organizations get their starter records from
/// organization setup at registration (rule 42); a live host must never gain demo logins with a known password,
/// demo organizations, or fabricated payments, and must never touch an organization it did not create. Startup
/// still applies migrations before seeding.
/// </summary>
internal sealed class NoDataSeeder : IDatabaseSeeder
{
    public Task SeedDatabaseAsync(IApplicationDbContext context, IOrganizationAccessor organizationAccessor)
        => Task.CompletedTask;
}
