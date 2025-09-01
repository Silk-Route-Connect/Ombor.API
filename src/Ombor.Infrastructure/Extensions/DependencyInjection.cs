using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Persistence;
using Ombor.Infrastructure.Services;
using Ombor.Infrastructure.Sms;
using Ombor.Infrastructure.Storage;

namespace Ombor.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IApplicationDbContext, ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentityCore<UserAccount>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddTransient<IImageThumbnailer, ImageSharpThumbnailer>();
        services.AddTransient<IFileStorage, LocalFileStorage>();
        services.AddTransient<IFilePathProvider, LocalFilePathProvider>();
        services.AddScoped<ISmsService, SmsService>();

        return services;
    }
}
