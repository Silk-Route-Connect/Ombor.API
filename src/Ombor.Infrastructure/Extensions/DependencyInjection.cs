using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Infrastructure.Persistence;
using Ombor.Infrastructure.Persistence.Interceptors;
using Ombor.Infrastructure.Services;
using Ombor.Infrastructure.Storage;

namespace Ombor.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
        .AddDatabase(configuration)
        .AddAuthentication(configuration)
        .AddInMemoryCache()
        .AddKeyPersistence()
        .AddServices();

    private static IServiceCollection AddKeyPersistence(this IServiceCollection services)
    {
        // Persist the Data Protection key ring in SQL Server instead of the default ephemeral store, and pin a
        // fixed application name, so keys survive restarts and are shared across instances.
        services
            .AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>()
            .SetApplicationName("Ombor");

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<IApplicationDbContext, ApplicationDbContext>((serviceProvider, options) =>
            options
                // Split multi-collection includes into separate queries to avoid cartesian-explosion warnings.
                .UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        return services;
    }

    private static IServiceCollection AddInMemoryCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IRedisService, MemoryCache>();

        return services;
    }

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Unable to resolve JWT settings.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = jwtSettings.SecurityKey
                };
            });

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IOrganizationAccessor, HttpContextOrganizationAccessor>();

        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

        services.AddScoped<INumberSequenceAllocator, NumberSequenceAllocator>();

        services.AddTransient<IImageThumbnailer, ImageSharpThumbnailer>();

        services.AddTransient<IFileStorage, LocalFileStorage>();

        services.AddTransient<IFilePathProvider, LocalFilePathProvider>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddHttpClient<ISmsService, SmsService>();

        return services;
    }
}
