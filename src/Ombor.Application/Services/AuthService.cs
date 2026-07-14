using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    IRequestValidator validator,
    IOrganizationService organizationService,
    IOrganizationSetupService organizationSetupService,
    IOptions<JwtSettings> jwtSettings) : IAuthService
{
    private const int ResetCodeLifetimeMinutes = 5;

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

        // disable for testing
        await smsService.SendMessageAsync(message);

        return new RegisterResponse("Registration OTP code sent to your phone number.", 5);
    }

    public async Task<VerifyOtpResponse> VerifyRegistrationOtpAsync(SmsVerificationRequest request, string language)
    {
        ArgumentNullException.ThrowIfNull(request);

        var otpCode = await otpCodeProvider.GetOtpAsync(request.PhoneNumber, OtpPurpose.Registration);

        if (!TryVerifyOtp(request, otpCode, out var response))
        {
            return response;
        }

        var registerRequest = await otpCodeProvider.GetRegisterRequestAsync(request.PhoneNumber);

        if (registerRequest is null)
        {
            return new VerifyOtpResponse();
        }

        var passwordHash = passwordHasher.HashPassword(registerRequest.Password);

        // Org + user + starter data + refresh token are one account: a mid-way failure must not leave an
        // orphan organization (which a retry can't reuse). Commit them atomically.
        User newUser;
        string refreshToken;
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                var organization = await organizationService.CreateAsync(registerRequest.OrganizationName);

                newUser = new User
                {
                    FirstName = registerRequest.FirstName,
                    LastName = registerRequest.LastName,
                    TelegramAccount = registerRequest.TelegramAccount,
                    PhoneNumber = registerRequest.PhoneNumber,
                    Email = registerRequest.Email,
                    PasswordHash = passwordHash.Hash,
                    PasswordSalt = passwordHash.Salt,
                    IsPhoneNumberConfirmed = true,
                    // The interface language chosen at registration (validated upstream from the request header).
                    Language = language,
                    OrganizationId = organization.Id,
                    Organization = null! // To be set by EF Core
                };

                context.Users.Add(newUser);
                await context.SaveChangesAsync();

                // Seed the organization's ordinary starter records (rule 42), named in the registration language.
                await organizationSetupService.SeedStarterDataAsync(organization.Id, language);

                refreshToken = tokenService.GenerateRefreshToken();
                await SaveRefreshTokenAsync(newUser, refreshToken);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Clear the OTP only after the account is durably committed, so a failed attempt can be retried.
        await otpCodeProvider.RemoveOtpAsync(request.PhoneNumber, OtpPurpose.Registration);
        await otpCodeProvider.RemoveRegisterRequestAsync(request.PhoneNumber);

        var accessToken = tokenService.GenerateAccessToken(newUser);

        return new VerifyOtpResponse(refreshToken, accessToken);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetOrThrowAsync(request.PhoneNumber);

        VerifyPassword(user, request.Password);

        // Deactivated users keep their audit history but cannot authenticate (rule 41).
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("This account has been deactivated.");
        }

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
            ExpiresAt = DateTime.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiresInDays),
            UserId = user.Id,
            User = user
        };

        context.RefreshTokens.Add(tokenEntity);
        await context.SaveChangesAsync();
    }

    private async Task<User> GetOrThrowAsync(string phoneNumber) =>
       await context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber)
       ?? throw new EntityNotFoundException<User>(phoneNumber);

    // Bypasses the organization query filter: refresh is anonymous (no org claim) and the user is fetched by
    // its own id. Without this, a stray org context filters the required User out and token refresh fails.
    private async Task<User> GetOrThrowAsync(int id) =>
       await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id)
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

    public async Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RefreshToken);

        var entity = await context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

        if (entity is null)
        {
            return;
        }

        if (!entity.IsRevoked)
        {
            entity.IsRevoked = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // Generic acknowledgement whether or not the number has an account, so the response never reveals
        // which phone numbers are registered (owner decision). The OTP + SMS only go out for a real user.
        var user = await context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (user is not null)
        {
            var code = await otpCodeProvider.GenerateOtpAsync(request.PhoneNumber, OtpPurpose.PasswordReset, ResetCodeLifetimeMinutes);

            var message = new SmsMessage(
                request.PhoneNumber,
                $"Inventory Management parolini tiklash uchun tasdiqlash kodi: {code}. Kod {ResetCodeLifetimeMinutes} daqiqa ichida amal qiladi, uni hech kim bilan ulashmang.",
                "Inventory Management");

            await smsService.SendMessageAsync(message);
        }

        return new ForgotPasswordResponse(
            "If an account exists for this number, a reset code has been sent.",
            ResetCodeLifetimeMinutes);
    }

    public async Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var otp = await otpCodeProvider.GetOtpAsync(request.PhoneNumber, OtpPurpose.PasswordReset);

        return IsOtpValid(otp, request.Code)
            ? new VerifyResetCodeResponse(true)
            : new VerifyResetCodeResponse(false, "The reset code is invalid or has expired.");
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var otp = await otpCodeProvider.GetOtpAsync(request.PhoneNumber, OtpPurpose.PasswordReset);

        if (!IsOtpValid(otp, request.Code))
        {
            return new ResetPasswordResponse(false, "The reset code is invalid or has expired.");
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (user is null)
        {
            // The OTP is only ever issued for a real account, so a missing user means a stale/forged code.
            return new ResetPasswordResponse(false, "The reset code is invalid or has expired.");
        }

        var passwordHash = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = passwordHash.Hash;
        user.PasswordSalt = passwordHash.Salt;

        // Force a fresh login everywhere after a password change (owner decision): a session opened before the
        // reset — including one an attacker may hold — must not survive it.
        var activeTokens = await context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        await context.SaveChangesAsync();
        await otpCodeProvider.RemoveOtpAsync(request.PhoneNumber, OtpPurpose.PasswordReset);

        return new ResetPasswordResponse(true, "Your password has been reset.");
    }

    // A reset OTP is valid when it exists, has not expired, and matches. Purpose-agnostic sibling of TryVerifyOtp.
    private static bool IsOtpValid(OtpCode? otp, string code) =>
        otp is not null && DateTime.UtcNow <= otp.ExpiredAt && otp.Code == code;

    private static bool TryVerifyOtp(
        SmsVerificationRequest request,
        OtpCode? otpData,
        [NotNullWhen(false)] out VerifyOtpResponse? failResponse)
    {
        failResponse = otpData switch
        {
            null => new VerifyOtpResponse(),
            { ExpiredAt: var exp } when DateTime.UtcNow > exp => new VerifyOtpResponse(),
            { Code: var code } when code != request.Code => new VerifyOtpResponse(),
            _ => null
        };

        return failResponse is null;
    }
}