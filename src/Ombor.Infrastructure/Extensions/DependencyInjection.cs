using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Infrastructure.Persistence;
using Ombor.Infrastructure.Persistence.Interceptors;
using Ombor.Infrastructure.Services;
using Ombor.Infrastructure.Storage;

namespace Ombor.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<LedgerEntryInterceptor>();

        services.AddDbContext<IApplicationDbContext, ApplicationDbContext>((serviceProvider, options) =>
            options
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .AddInterceptors(serviceProvider.GetRequiredService<LedgerEntryInterceptor>()));

        services.AddTransient<IImageThumbnailer, ImageSharpThumbnailer>();
        services.AddTransient<IFileStorage, LocalFileStorage>();
        services.AddTransient<IFilePathProvider, LocalFilePathProvider>();
        services.AddTransient<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
