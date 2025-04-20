using FluentValidation.Results;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Defines a contract for validating request objects.
/// </summary>
public interface IRequestValidator
{
    /// <summary>
    /// Validates the specified request synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the request object to validate.</typeparam>
    /// <param name="request">The instance of <typeparamref name="T"/> to validate.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> that indicates whether the request is valid and contains any validation failures.
    /// </returns>
    void ValidateAndThrow<T>(T request);

    /// <summary>
    /// Validates the specified request asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the request object to validate.</typeparam>
    /// <param name="request">The instance of <typeparamref name="T"/> to validate.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task{ValidationResult}"/> that, when completed, returns a
    /// <see cref="ValidationResult"/> indicating whether the request is valid
    /// and containing any validation failures.
    /// </returns>
    Task ValidateAndThrowAsync<T>(T request, CancellationToken cancellationToken);
}
