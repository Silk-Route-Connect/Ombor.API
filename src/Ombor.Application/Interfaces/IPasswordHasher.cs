using Ombor.Application.Models;
using Ombor.Domain.Entities;

namespace Ombor.Application.Interfaces;

public interface IPasswordHasher
{
    PasswordHash HashPassword(string password);
    bool VerifyPassword(string password, User user);
}
