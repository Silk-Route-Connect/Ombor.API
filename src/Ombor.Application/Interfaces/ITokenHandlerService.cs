using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface ITokenHandlerService
{
    string GenerateAccessToken(UserAccount user);
    string GenerateRefreshToken();
    string GenerateVerificationToken(UserAccount user, string code);
}
