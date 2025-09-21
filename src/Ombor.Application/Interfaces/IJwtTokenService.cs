using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string GenerateVerificationToken(User user, string code);
}
