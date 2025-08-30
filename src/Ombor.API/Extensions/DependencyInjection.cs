using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Ombor.API.ExceptionHandlers;
using Ombor.API.Filters;

namespace Ombor.API.Extensions;

internal static class DependencyInjection
{
    public const string CorsPolicyName = "DefaultCorsPolicy";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.SuppressAsyncSuffixInActionNames = false)
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSwagger(configuration);
        services.AddErrorHandlers();
        services.AddCors(configuration);

        return services;
    }

    private static void AddErrorHandlers(this IServiceCollection services)
    {
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<EntityNotFoundExceptionHandler>();
        services.AddExceptionHandler<InvalidFileExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
    }

    private static void AddSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var openApiConfigurations = new OpenApiInfo();
        configuration.GetSection("Swagger").Bind(openApiConfigurations);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", openApiConfigurations);

            c.OperationFilter<ValidationErrorsOperationFilter>();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);

            c.SchemaFilter<EnumSchemaFilter>();
        }).AddSwaggerGenNewtonsoftSupport();
    }

    private static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Cannot configure CORS without allowed origins specified.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
