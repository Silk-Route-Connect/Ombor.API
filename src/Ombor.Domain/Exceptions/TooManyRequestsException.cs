namespace Ombor.Domain.Exceptions;

/// <summary>
/// A request refused by a throttle (OTP resend cooldown, daily cap, login lockout). Surfaces as HTTP 429 with a
/// <c>Retry-After</c> header and <c>params.retryAfterSeconds</c>.
/// </summary>
public sealed class TooManyRequestsException : Exception, ICodedError
{
    public TooManyRequestsException(TimeSpan retryAfter, string message = "Too many requests. Try again later.")
        : base(message)
    {
        // Never advertise "retry in 0 s": round up so the client always waits at least a second.
        RetryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
    }

    public int RetryAfterSeconds { get; }

    public string Code => ErrorCodes.RateLimited;

    public IReadOnlyDictionary<string, object?>? Params =>
        new Dictionary<string, object?> { ["retryAfterSeconds"] = RetryAfterSeconds };
}
