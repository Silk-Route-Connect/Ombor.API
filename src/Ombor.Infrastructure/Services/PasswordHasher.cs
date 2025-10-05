using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Services;

internal sealed class PasswordHasher : IPasswordHasher
{
    public PasswordHash HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hashBytes = KeyDerivation.Pbkdf2(
            password: password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 310_000,
            numBytesRequested: 32);

        return new PasswordHash(
            Hash: Convert.ToBase64String(hashBytes),
            Salt: Convert.ToBase64String(saltBytes)
            );
    }

    public bool VerifyPassword(string password, User user)
    {
        var saltBytes = Convert.FromBase64String(user.PasswordSalt);
        var expectedHashBytes = Convert.FromBase64String(user.PasswordHash);

        var actualHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 310_000,
            numBytesRequested: expectedHashBytes.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHashBytes);
    }
}
