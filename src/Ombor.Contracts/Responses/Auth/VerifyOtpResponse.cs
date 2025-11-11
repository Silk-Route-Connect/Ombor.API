namespace Ombor.Contracts.Responses.Auth;

public sealed record VerifyOtpResponse
{
    public bool Success { get; }
    public string? RefreshToken { get; init; }
    public string? AccessToken { get; init; }

    public VerifyOtpResponse(string refreshToken, string accessToken)
    {
        RefreshToken = refreshToken;
        AccessToken = accessToken;
        Success = true;
    }

    public VerifyOtpResponse()
    {
        Success = false;
        RefreshToken = null;
        AccessToken = null;
    }
}
