using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;

namespace Ombor.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<VerifyOtpResponse> VerifyRegistrationOtpAsync(SmsVerificationRequest request);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request);
}
