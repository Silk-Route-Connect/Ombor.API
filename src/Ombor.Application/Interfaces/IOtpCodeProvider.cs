using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Domain.Enums;

namespace Ombor.Application.Interfaces;

public interface IOtpCodeProvider
{
    Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose, int lifetimeInMinutes);
    Task<OtpCode?> GetOtpAsync(string phoneNumber, OtpPurpose purpose);
    Task<RegisterRequest?> GetRegisterRequestAsync(string phoneNumber);
    Task SetRegisterRequestAsync(RegisterRequest request, TimeSpan lifetime);
    Task RemoveOtpAsync(string phoneNumber, OtpPurpose purpose);
    Task RemoveRegisterRequestAsync(string phoneNumber);
}
