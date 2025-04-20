using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;
using Ombor.Application.Services;

namespace Ombor.Application.Extensions;

public static class DependencyInjection
{
    private static Assembly CurrentAssembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddFluentValidationAutoValidation()
            .AddFluentValidationClientsideAdapters()
            .AddValidatorsFromAssembly(CurrentAssembly);

        services.AddScoped<IRequestValidator, RequestValidator>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();


        return services;
    }
}
