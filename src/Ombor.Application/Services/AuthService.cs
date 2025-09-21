using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class AuthService(
    IApplicationDbContext context,
    ISmsService smsService,
    IJwtTokenService tokenService,
    IRedisService redisService,
    IPasswordHasher passwordHasher,
    IConfiguration configuration) : IAuthService
{
    public async Task<SendOtpResponse> RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (existingUser is not null)
        {
            throw new ArgumentException("User with this phone number already exists.");
        }

        var (hash, salt) = passwordHasher.HashPassword(request.Password);

        var newUser = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            TelegramAccount = request.TelegramAccount,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsPhoneNumberConfirmed = false
        };

        var key = $"reg:user:${newUser.PhoneNumber}";
        await redisService.SetAsync<User>(key, newUser, TimeSpan.FromMinutes(10));

        var code = await GenerateOtpCode(request.PhoneNumber);

        var message = new SmsMessage
        (
            request.PhoneNumber,
            $"Bu Eskiz dan test",
            "Bu Eskiz dan test"
        );

        await smsService.SendMessageAsync(message);

        return new SendOtpResponse("Registration OTP code sent to your phone number.", 5);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByPhoneNumberAsync(request.PhoneNumber);

        CheckUser(user, request.Password);

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user, refreshToken);

        return new LoginResponse(accessToken, refreshToken);
    }

    public async Task<VerificationResponse> VerifyRegistrationOtpAsync(SmsVerificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var otpKey = $"otp:{request.PhoneNumber}:{OtpPurpose.Registration}";
        var otpData = await redisService.GetAsync<OtpCode>(otpKey);

        if (otpData is null)
        {
            return new VerificationResponse(false, "OTP expired or not found");
        }

        if (DateTime.UtcNow > otpData.ExpiresAt)
        {
            return new VerificationResponse(false, "OTP expired");
        }

        if (otpData.Code != request.Code)
        {
            return new VerificationResponse(false, "The code is incorrect.");
        }

        var userKey = $"reg:user:${request.PhoneNumber}";
        var user = await redisService.GetAsync<User>(userKey);

        if (user is null)
        {
            return new VerificationResponse(false, "User not found");
        }

        user.IsPhoneNumberConfirmed = true;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await redisService.RemoveAsync(otpKey);
        await redisService.RemoveAsync(userKey);

        return new VerificationResponse(true, "Phone number successfully verified");
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var refreshToken = await context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

        if (refreshToken is null || refreshToken.IsRevoked)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            refreshToken.IsRevoked = true;

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == refreshToken.UserId);

        if (user is null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        var newAccessToken = tokenService.GenerateAccessToken(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user, newRefreshToken);

        return new LoginResponse(newAccessToken, newRefreshToken);

    }

    private async Task SaveRefreshTokenAsync(User user, string refreshToken)
    {
        var tokenEntity = new RefreshToken
        {
            Token = refreshToken,
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(configuration["Jwt:RefreshTokenExpiresInDays"]!)),
            UserId = user.Id,
            User = user
        };

        context.RefreshTokens.Add(tokenEntity);
        await context.SaveChangesAsync();
    }

    private async Task<string> GenerateOtpCode(string phoneNumber)
    {
        var code = RandomNumberGenerator.GetInt32(1000, 9999).ToString();

        var otpData = new OtpCode
        {
            PhoneNumber = phoneNumber,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Purpose = OtpPurpose.Registration
        };

        var key = $"otp:{otpData.PhoneNumber}:{otpData.Purpose}";

        await redisService.SetAsync(key, otpData, otpData.ExpiresAt - DateTime.UtcNow);

        Console.WriteLine($"[SMS] {phoneNumber} uchun code: {code}");

        return code;
    }

    private async Task<User> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        return user is null ? throw new InvalidOperationException("User not found") : user;
    }

    private void CheckUser(User user, string password)
    {
        if (!passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            throw new UnauthorizedAccessException("Invalid password");
        }

        if (!user.IsPhoneNumberConfirmed)
        {
            throw new UnauthorizedAccessException("Phone number is not confirmed");
        }
    }
}