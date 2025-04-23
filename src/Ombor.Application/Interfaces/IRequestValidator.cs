namespace Ombor.Application.Interfaces;

/// <summary>
/// Contract for validating request objects, throwing on failure.
/// </summary>
public interface IRequestValidator
{
    /// <summary>
    /// Validates the specified request synchronously, throwing a <see cref="ValidationException"/> on failure.
    /// </summary>
    /// <typeparam name="T">The request type.</typeparam>
    /// <param name="request">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="request"/> is null.</exception>
    /// <exception cref="ValidationException">If validation fails.</exception>
    void ValidateAndThrow<T>(T request);

    /// <summary>
    /// Validates the specified request asynchronously, throwing a <see cref="ValidationException"/> on failure.
    /// </summary>
    /// <typeparam name="T">The request type.</typeparam>
    /// <param name="request">The instance to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A completed <see cref="Task"/> on success.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="request"/> is null.</exception>
    /// <exception cref="ValidationException">If validation fails.</exception>
    Task ValidateAndThrowAsync<T>(T request, CancellationToken cancellationToken);
}