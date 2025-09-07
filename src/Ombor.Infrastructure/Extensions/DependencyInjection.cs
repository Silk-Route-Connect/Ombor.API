using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Infrastructure.Persistence;
using Ombor.Infrastructure.Services;
using Ombor.Infrastructure.Storage;

namespace Ombor.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IApplicationDbContext, ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services.AddTransient<IImageThumbnailer, ImageSharpThumbnailer>();
        services.AddTransient<IFileStorage, LocalFileStorage>();
        services.AddTransient<IFilePathProvider, LocalFilePathProvider>();

        return services;
    }
}
