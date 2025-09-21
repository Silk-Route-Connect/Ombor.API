using System.Security.Cryptography;
using System.Text;
using Ombor.Application.Interfaces;

namespace Ombor.Infrastructure.Services;

internal sealed class PasswordHasher : IPasswordHasher
{
    public (string hash, string salt) HashPassword(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = Convert.ToBase64String(hmac.Key);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));

        return (hash, salt);
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
        var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));

        return computedHash == storedHash;
    }
}
