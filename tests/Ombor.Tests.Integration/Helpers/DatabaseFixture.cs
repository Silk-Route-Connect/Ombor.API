using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Ombor.Tests.Integration.Helpers;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
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
                    .UseNpgsql(DatabaseConnectionString)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .EnableSensitiveDataLogging()
                    .Options;

                _context = new ApplicationDbContext(options);
            }

            return _context;
        }
    }

    public string DatabaseConnectionString => _postgresContainer.GetConnectionString();

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

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    private async Task<ApplicationDbContext> GetDbContext()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString)
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
