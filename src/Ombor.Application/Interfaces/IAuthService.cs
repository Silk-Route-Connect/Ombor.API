using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;

namespace Ombor.Application.Interfaces;

/// <summary>Registration (phone + one-time code), sign-in and session (refresh-token) lifecycle.</summary>
public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, string language);
    Task<AuthSession> LoginAsync(LoginRequest request);
    Task<RegistrationVerification> VerifyRegistrationOtpAsync(SmsVerificationRequest request, string language);
    Task<AuthSession> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request);
}
