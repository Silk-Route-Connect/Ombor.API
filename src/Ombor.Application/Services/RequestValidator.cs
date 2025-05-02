using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;

namespace Ombor.Application.Services;

internal sealed class RequestValidator(IServiceProvider serviceProvider) : IRequestValidator
{
    public void ValidateAndThrow<TRequest>(TRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validator = serviceProvider.GetRequiredService<IValidator<TRequest>>();
        var result = validator.Validate(request);

        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    public async Task ValidateAndThrowAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validator = serviceProvider.GetRequiredService<IValidator<TRequest>>();
        var result = await validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}
