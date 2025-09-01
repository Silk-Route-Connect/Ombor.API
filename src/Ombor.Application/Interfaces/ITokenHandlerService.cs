using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface ITokenHandlerService
{
    string GenerateVerificationToken(UserAccount user, string code);
}
