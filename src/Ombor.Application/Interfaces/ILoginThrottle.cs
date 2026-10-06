namespace Ombor.Application.Interfaces;

/// <summary>
/// Per-phone password-guess lockout (login and change-password). An attempt counts as failed until it succeeds,
/// so parallel guesses cannot slip past the limit: <see cref="BeginAttemptAsync"/> spends one attempt up front and
/// <see cref="ResetAsync"/> clears the count after a correct password.
/// </summary>
public interface ILoginThrottle
{
    /// <summary>
    /// Spends one attempt for <paramref name="phoneNumber"/>; throws
    /// <see cref="Ombor.Domain.Exceptions.TooManyRequestsException"/> once the window's failure budget is spent.
    /// </summary>
    Task BeginAttemptAsync(string phoneNumber);

    /// <summary>Clears the phone's failure count after a successful password check.</summary>
    Task ResetAsync(string phoneNumber);
}
