using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;

namespace Ombor.Application.Interfaces;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<bool> SmsVerificationAsync(SmsVerificationRequest request);
}
