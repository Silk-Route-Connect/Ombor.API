using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Ombor.Tests.Integration.Helpers;

public class DatabaseFixture : IAsyncLifetime
{
    private const string DatabaseName = "TestDB";

    private readonly MsSqlContainer _sqlServerContainer = new MsSqlBuilder()
        .WithCleanUp(true)
        .Build();

    private ApplicationDbContext? _context;
    public IApplicationDbContext Context
    {
        get
        {
            if (_context is null)
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(DatabaseConnectionString)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .EnableSensitiveDataLogging()
                    .Options;

                _context = new ApplicationDbContext(options);
            }

            return _context;
        }
    }

    public string DatabaseConnectionString
    {
        get
        {
            var connectionString = new SqlConnectionStringBuilder(_sqlServerContainer.GetConnectionString())
            {
                InitialCatalog = DatabaseName,
            };

            return connectionString.ConnectionString;
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            _context = await GetDbContext();

            await _context.Database.EnsureDeletedAsync();
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("There was an error intializing test database, {0}", ex.Message);
        }
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
        }

        if (_sqlServerContainer != null)
        {
            await _sqlServerContainer.DisposeAsync();
        }
    }

    private async Task<ApplicationDbContext> GetDbContext()
    {
        await _sqlServerContainer.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DatabaseConnectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new ApplicationDbContext(options);
    }
}

[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
