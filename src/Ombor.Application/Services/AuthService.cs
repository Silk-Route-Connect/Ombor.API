using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class AuthService(
    UserManager<UserAccount> userManager,
    ISmsService smsService,
    ITokenHandlerService tokenHandler,
    IConfiguration configuration) : IAuthService
{
    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<string> RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Password and Confirm Password do not match.");
        }

        var existingUser = await userManager.FindByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            throw new ArgumentException("User with this phone number already exists.");
        }

        var newUser = new UserAccount
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            TelegramAccount = request.TelegramAccount,
            UserName = request.PhoneNumber,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Access = UserAccess.BasicAccess
        };

        var result = await userManager.CreateAsync(newUser, request.Password);

        if (!result.Succeeded)
        {
            throw new ArgumentException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var verificationCode = new Random().Next(1000, 9999).ToString();

        var jwt = tokenHandler.GenerateVerificationToken(newUser, verificationCode);

        var message = new SmsMessage
        (
            request.PhoneNumber,
            $"Код подтверждения: {verificationCode}",
            "Добро пожаловать"
        );

        await smsService.SendMessageAsync(message);

        return jwt;
    }

    public async Task<bool> SmsVerificationAsync(SmsVerificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

        try
        {
            var principal = tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            if (!int.TryParse(principal
                .Claims.
                FirstOrDefault(x => x.Type == "UserId")?.Value, out int userId))
            {
                return false;
            }
            var verificationCode = principal.Claims.FirstOrDefault(x => x.Type == "VerificationCode")?.Value;

            var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                return false;
            }

            return verificationCode == request.Code;
        }
        catch
        {
            return false;
        }

    }
}
