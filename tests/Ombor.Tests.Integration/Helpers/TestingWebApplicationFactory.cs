using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers.ResponseValidators;

namespace Ombor.Tests.Integration.Helpers;

public class TestingWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _databaseFixture;

    private ResponseValidator _responseValidator;
    public ResponseValidator ResponseValidator
    {
        get => _responseValidator ??= new ResponseValidator(_databaseFixture.Context);
    }

    public IApplicationDbContext Context => _databaseFixture.Context;

    public TestingWebApplicationFactory(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;

        _responseValidator = new ResponseValidator(_databaseFixture.Context);
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

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_databaseFixture.SqlServerConnectionString));
        });

        builder.UseEnvironment("Testing");
    }
}
