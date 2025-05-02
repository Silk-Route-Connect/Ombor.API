namespace Ombor.Application.Interfaces;

/// <summary>
/// Contract for validating request objects, throwing on failure.
/// </summary>
public interface IRequestValidator
{
    /// <summary>
    /// Validates the specified <paramref name="request"/>.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request to validate.</typeparam>
    /// <param name="request">The instance to validate. Cannot be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="request"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no <see cref="FluentValidation.IValidator{TRequest}"/> is registered for <typeparamref name="TRequest"/>.
    /// </exception>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown if one or more validation rules fail.
    /// </exception>
    void ValidateAndThrow<TRequest>(TRequest request);

    /// <summary>
    /// Validates the specified <paramref name="request"/> asynchronously.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request to validate.</typeparam>
    /// <param name="request">The instance to validate. Cannot be <c>null</c>.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while waiting for the validation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that completes if validation succeeds, or faults with
    /// <see cref="FluentValidation.ValidationException"/> if validation fails.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="request"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no <see cref="FluentValidation.IValidator{TRequest}"/> is registered for <typeparamref name="TRequest"/>.
    /// </exception>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown if one or more validation rules fail.
    /// </exception>
    Task ValidateAndThrowAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default);
}