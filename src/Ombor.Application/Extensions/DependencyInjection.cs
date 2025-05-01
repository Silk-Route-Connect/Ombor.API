using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;
using Ombor.Application.Services;

namespace Ombor.Application.Extensions;

/// <summary>
/// Registers application‑layer services and FluentValidation adapters.
/// </summary>
public static class DependencyInjection
{
    private static Assembly CurrentAssembly => Assembly.GetExecutingAssembly();

    /// <summary>
    /// Adds product and category services and request validators to the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(CurrentAssembly);

        services.AddScoped<IRequestValidator, RequestValidator>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();

        return services;
    }
}
