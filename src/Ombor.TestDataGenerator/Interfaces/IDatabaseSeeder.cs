using Ombor.Application.Interfaces;

namespace Ombor.TestDataGenerator.Interfaces;

public interface IDatabaseSeeder
{
    /// <param name="organizationSetup">
    /// Registration's starter-record setup, from the same scope as <paramref name="context"/>; the development seed
    /// gives demo organizations the same starter rows a registered one has.
    /// </param>
    Task SeedDatabaseAsync(
        IApplicationDbContext context,
        IOrganizationAccessor organizationAccessor,
        IOrganizationSetupService organizationSetup);
}
