using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;

namespace Ombor.Application.Interfaces;

public interface IAuthService
{
    Task<SendOtpResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<VerificationResponse> VerifyRegistrationOtpAsync(SmsVerificationRequest request);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
