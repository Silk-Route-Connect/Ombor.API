namespace Ombor.Contracts.Responses.Auth;

/// <summary>
/// The registration code was confirmed and the account created: the first session's access token. The refresh token
/// travels only in the httpOnly <c>ombor.refreshToken</c> cookie, never in the body. A refused code is a 400
/// <c>{ message, code }</c> body instead.
/// </summary>
/// <param name="AccessToken">The JWT access token.</param>
public sealed record VerifyOtpResponse(string AccessToken)
{
    /// <summary>Always true: a refused code never produces this body.</summary>
    public bool Success => true;
}
