using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class AuthService(
    IApplicationDbContext context,
    ISmsService smsService,
    IJwtTokenService tokenService,
    IOtpCodeProvider otpCodeProvider,
    IPasswordHasher passwordHasher,
    IOrganizationService organizationService,
    JwtSettings jwtSettings) : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (existingUser is not null)
        {
            throw new InvalidOperationException("User with this phone number already exists.");
        }

        await otpCodeProvider.SetRegisterRequestAsync(request, TimeSpan.FromMinutes(5));
        var code = await otpCodeProvider.GenerateOtpAsync(request.PhoneNumber, OtpPurpose.Registration, 5);

        var message = new SmsMessage
        (
            request.PhoneNumber,
            $"Inventory Management tizimiga ro‘yxatdan o‘tish uchun tasdiqlash kodi: {code}. Eslatma: Kod 5 daqiqa ichida amal qiladi, uni hech kim bilan ulashmang.",
            "Inventory Management"
        );

        await smsService.SendMessageAsync(message);

        return new RegisterResponse("Registration OTP code sent to your phone number.", 5);
    }

    public async Task<VerifyOtpResponse> VerifyRegistrationOtpAsync(SmsVerificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var otpCode = await otpCodeProvider.GetOtpAsync(request.PhoneNumber, OtpPurpose.Registration);

        if (!TryVerifyOtp(request, otpCode!, out var response))
        {
            return response;
        }

        var registerRequest = await otpCodeProvider.GetRegisterRequestAsync(request.PhoneNumber);

        if (registerRequest is null)
        {
            return new VerifyOtpResponse(false, "Registration request not found or has expired");
        }

        var organization = await organizationService.CreateAsync(registerRequest.OrganizationName);

        var passwordHash = passwordHasher.HashPassword(registerRequest.Password);

        var newUser = new User
        {
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            TelegramAccount = registerRequest.TelegramAccount,
            PhoneNumber = registerRequest.PhoneNumber,
            Email = registerRequest.Email,
            PasswordHash = passwordHash.Hash,
            PasswordSalt = passwordHash.Salt,
            IsPhoneNumberConfirmed = true,
            OrganizationId = organization.Id,
            Organization = null! // To be set by EF Core
        };

        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        await otpCodeProvider.RemoveOtpAsync(request.PhoneNumber, OtpPurpose.Registration);
        await otpCodeProvider.RemoveRegisterRequestAsync(request.PhoneNumber);

        return new VerifyOtpResponse(true, "Phone number successfully verified");
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetOrThrowAsync(request.PhoneNumber);

        VerifyPassword(user, request.Password);

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        await SaveRefreshTokenAsync(user, refreshToken);

        return new LoginResponse(accessToken, refreshToken);
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
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
            await context.SaveChangesAsync();

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await GetOrThrowAsync(refreshToken.UserId);

        var newAccessToken = tokenService.GenerateAccessToken(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        refreshToken.IsRevoked = true;
        await context.SaveChangesAsync();

        await SaveRefreshTokenAsync(user, newRefreshToken);

        return new RefreshTokenResponse(newAccessToken, newRefreshToken);
    }

    private async Task SaveRefreshTokenAsync(User user, string refreshToken)
    {
        var tokenEntity = new RefreshToken
        {
            Token = refreshToken,
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpiresInDays),
            UserId = user.Id,
            User = user
        };

        context.RefreshTokens.Add(tokenEntity);
        await context.SaveChangesAsync();
    }

    private async Task<User> GetOrThrowAsync(string phoneNumber) =>
       await context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber)
       ?? throw new EntityNotFoundException<User>(phoneNumber);

    private async Task<User> GetOrThrowAsync(int id) =>
       await context.Users.FirstOrDefaultAsync(x => x.Id == id)
       ?? throw new EntityNotFoundException<User>(id);

    private void VerifyPassword(User user, string password)
    {
        if (!passwordHasher.VerifyPassword(password, user))
        {
            throw new UnauthorizedAccessException("Invalid phone number or password.");
        }

        if (!user.IsPhoneNumberConfirmed)
        {
            throw new UnauthorizedAccessException("Invalid phone number or password.");
        }
    }

    private static bool TryVerifyOtp(SmsVerificationRequest request, OtpCode otpData,
        [NotNullWhen(false)] out VerifyOtpResponse? failResponse)
    {
        failResponse = otpData switch
        {
            null => new VerifyOtpResponse(false, "OTP expired or not found"),
            { ExpiredAt: var exp } when DateTime.UtcNow > exp
                => new VerifyOtpResponse(false, "OTP expired"),
            { Code: var code } when code != request.Code
                => new VerifyOtpResponse(false, "The code is incorrect."),
            _ => null
        };

        return failResponse is null;
    }
}