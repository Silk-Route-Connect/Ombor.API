using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;

namespace Ombor.Application.Services;

internal sealed class RequestValidator(IServiceProvider serviceProvider) : IRequestValidator
{
    public async Task ValidateAndThrowAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            // A missing/null request body is a client error — surface it as a 400, not a 500.
            throw new ValidationException("Request body is required.");
        }

        var validator = serviceProvider.GetRequiredService<IValidator<TRequest>>();
        var result = await validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}
