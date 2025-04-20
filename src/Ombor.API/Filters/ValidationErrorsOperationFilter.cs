using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ombor.API.Filters;

public class ValidationErrorsOperationFilter(IServiceScopeFactory scopeFactory) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var bodyParam = context.MethodInfo
            .GetParameters()
            .FirstOrDefault(p => p.GetCustomAttribute<FromBodyAttribute>() != null);
        var bodyType = bodyParam?.ParameterType;
        if (bodyType is null)
        {
            return;
        }

        var validatorServiceType = typeof(IValidator<>).MakeGenericType(bodyType);

        using var scope = scopeFactory.CreateScope();
        if (scope.ServiceProvider.GetService(validatorServiceType) is not IValidator)
        {
            return;
        }

        operation.Responses["400"] = new OpenApiResponse
        {
            Description = "Validation errors",
            Content =
            {
                ["application/problem+json"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(
                        typeof(ValidationProblemDetails),
                        context.SchemaRepository)
                }
            }
        };

        operation.Responses["400"].Content["application/problem+json"].Examples =
            new Dictionary<string, OpenApiExample>
            {
                ["SampleValidationError"] = new OpenApiExample
                {
                    Summary = "Missing or invalid fields",
                    Value = OpenApiAnyFactory.CreateFromJson("""
                    {
                        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        "title": "One or more validation errors occurred.",
                        "status": 400,
                        "errors": {
                            "Name": [ "Name is required." ],
                            "Description": [ "Description must not exceed 500 characters." ]
                        }
                    }
                    """)
                }
            };
    }
}
