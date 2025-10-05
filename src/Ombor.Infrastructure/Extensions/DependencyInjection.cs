using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Infrastructure.Persistence;
using Ombor.Infrastructure.Services;
using Ombor.Infrastructure.Storage;
using StackExchange.Redis;

namespace Ombor.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton(x => x.GetRequiredService<IOptions<JwtSettings>>().Value);

        services.Configure<SmsSettings>(configuration.GetSection(SmsSettings.SectionName));
        services.AddSingleton(x => x.GetRequiredService<IOptions<SmsSettings>>().Value);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Redis connection string is missing");

            var cfg = ConfigurationOptions.Parse(connectionString);
            cfg.AbortOnConnectFail = false;
            cfg.ConnectRetry = 3;
            cfg.ConnectTimeout = 5000;

            return ConnectionMultiplexer.Connect(cfg);
        });

        services.AddScoped<IRedisService, RedisService>();

        services.AddTransient<IImageThumbnailer, ImageSharpThumbnailer>();
        services.AddTransient<IFileStorage, LocalFileStorage>();
        services.AddTransient<IFilePathProvider, LocalFilePathProvider>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRedisService, RedisService>();

        services.AddHttpClient<ISmsService, SmsService>();

        return services;
    }
}
