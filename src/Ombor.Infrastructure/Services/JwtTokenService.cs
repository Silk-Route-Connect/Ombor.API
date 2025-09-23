using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Services;

internal sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FirstName),
        };

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
        }

        var credentials = GetCredentials();
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var hours = configuration.GetValue<double>("Jwt:AccessTokenExpiresInHours");

        if (hours <= 0)
        {
            throw new InvalidOperationException("Jwt:AccessTokenExpiresInHours must be > 0.");
        }

        var securityToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

        return token;
    }

    public string GenerateRefreshToken()
    {
        var randomNumbers = new byte[32];
        using var randomGenerator = RandomNumberGenerator.Create();

        randomGenerator.GetBytes(randomNumbers);

        var token = Convert.ToBase64String(randomNumbers);

        return token;
    }

    private SigningCredentials GetCredentials()
    {
        var key = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Jwt:Key is missing.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(key);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 256 bits.");
        }

        var securityKey = new SymmetricSecurityKey(keyBytes);
        var signingKey = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        return signingKey;
    }
}
