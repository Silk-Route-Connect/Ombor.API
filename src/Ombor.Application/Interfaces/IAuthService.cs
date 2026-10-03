using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;

namespace Ombor.Application.Interfaces;

/// <summary>Registration (phone + one-time code), sign-in and session (refresh-token) lifecycle.</summary>
public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<VerifyOtpResponse> VerifyRegistrationOtpAsync(SmsVerificationRequest request, string language);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request);
}
