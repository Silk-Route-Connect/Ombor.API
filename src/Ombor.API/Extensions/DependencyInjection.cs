using System.Reflection;
using Microsoft.OpenApi.Models;
using Ombor.API.ExceptionHandlers;
using Ombor.API.Filters;

namespace Ombor.API.Extensions;

internal static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddSwagger(configuration);
        services.AddErrorHandlers();

        return services;
    }

    private static void AddErrorHandlers(this IServiceCollection services)
    {
        services.AddExceptionHandler<EntityNotFoundExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
    }

    private static void AddSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Inventory Management System",
                Version = "v1",
                Description = "Manage categories, products, orders, etc.",
                Contact = new OpenApiContact { Name = "Support Engineer", Email = "support@silkrouteconnect.com" },
                License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
            });

            c.OperationFilter<ValidationErrorsOperationFilter>();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        }).AddSwaggerGenNewtonsoftSupport();
    }
}
