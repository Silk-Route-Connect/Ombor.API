using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;

namespace Ombor.Application.Services;

internal sealed class RequestValidator(IServiceProvider serviceProvider) : IRequestValidator
{
    public void ValidateAndThrow<T>(T request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validator = GetValidator<T>();

        validator.ValidateAndThrow(request);
    }

    public Task ValidateAndThrowAsync<T>(T request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validator = GetValidator<T>();

        return validator.ValidateAndThrowAsync(request, cancellationToken);
    }

    private IValidator<T> GetValidator<T>() =>
        serviceProvider.GetRequiredService<IValidator<T>>()
        ?? throw new InvalidOperationException($"Validator for type {typeof(T).Name} not found.");
}
