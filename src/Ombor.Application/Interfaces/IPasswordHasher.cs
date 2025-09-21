namespace Ombor.Application.Interfaces;

public interface IPasswordHasher
{
    (string hash, string salt) HashPassword(string password);
    bool VerifyPassword(string password, string storedHash, string storedSalt);
}
