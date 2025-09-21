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
    public string GenerateVerificationToken(User user, string code)
    {

        var credentials = GetCredentials();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
            new Claim("UserId",user.Id.ToString()),
            new Claim("VerificationCode",code),
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials
            );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
            new Claim(ClaimTypes.Name,user.FirstName),
            new Claim(ClaimTypes.MobilePhone,user.PhoneNumber!)
        };

        var credentials = GetCredentials();
        var securityToken = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(configuration["Jwt:AccessTokenExpiresInHours"]!)),
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
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var signingKey = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        return signingKey;
    }
}
