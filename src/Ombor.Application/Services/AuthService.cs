using Microsoft.EntityFrameworkCore;
using Ombor.Application.Configurations;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Application.Validators;
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
    IRefreshTokenStore refreshTokens,
    ILoginThrottle loginThrottle) : IAuthService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(OtpSettings.CodeLifetimeMinutes);

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // Cache the canonical form: verification creates the account from exactly this request.
        request = request with
        {
            PhoneNumber = PhoneNumbers.Canonical(request.PhoneNumber),
            Email = NullIfBlank(request.Email),
            TelegramAccount = NullIfBlank(request.TelegramAccount),
        };

        await EnsureAccountDetailsAvailableAsync(request);
        await otpCodeProvider.EnsureCanSendAsync(request.PhoneNumber, OtpPurpose.Registration);

        await otpCodeProvider.SetRegisterRequestAsync(request, CodeLifetime);
        var code = await otpCodeProvider.GenerateOtpAsync(request.PhoneNumber, OtpPurpose.Registration);

        var message = new SmsMessage
        (
            request.PhoneNumber,
            $"Inventory Management tizimiga ro‘yxatdan o‘tish uchun tasdiqlash kodi: {code}. Eslatma: Kod 5 daqiqa ichida amal qiladi, uni hech kim bilan ulashmang.",
            "Inventory Management"
        );

        await smsService.SendMessageAsync(message);

        return new RegisterResponse(
            "Registration OTP code sent to your phone number.",
            OtpSettings.CodeLifetimeMinutes,
            otpCodeProvider.CodeLength,
            otpCodeProvider.ResendAfterSeconds);
    }

    public async Task<RegistrationVerification> VerifyRegistrationOtpAsync(SmsVerificationRequest request, string language)
    {
        await validator.ValidateAndThrowAsync(request);

        var phoneNumber = PhoneNumbers.Canonical(request.PhoneNumber);
        var check = await otpCodeProvider.VerifyOtpAsync(phoneNumber, OtpPurpose.Registration, request.Code);

        if (check != OtpCheckResult.Valid)
        {
            return new RegistrationVerification(null, check.ToErrorCode());
        }

        var registerRequest = await otpCodeProvider.GetRegisterRequestAsync(phoneNumber);

        if (registerRequest is null)
        {
            return new RegistrationVerification(null, ErrorCodes.CodeExpired);
        }

        // Another registration may have claimed the phone or email while this code was in flight.
        await EnsureAccountDetailsAvailableAsync(registerRequest);

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

                refreshToken = await refreshTokens.IssueAsync(newUser);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Clear the OTP only after the account is durably committed, so a failed attempt can be retried.
        await otpCodeProvider.RemoveOtpAsync(phoneNumber, OtpPurpose.Registration);
        await otpCodeProvider.RemoveRegisterRequestAsync(phoneNumber);

        var accessToken = tokenService.GenerateAccessToken(newUser);

        return new RegistrationVerification(new AuthSession(accessToken, refreshToken, newUser.Language), null);
    }

    public async Task<AuthSession> LoginAsync(LoginRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        // A number that cannot be normalized cannot match an account: the same 401 as a wrong password.
        if (!PhoneNumbers.TryNormalize(request.PhoneNumber, out var phoneNumber))
        {
            throw InvalidCredentials();
        }

        await loginThrottle.BeginAttemptAsync(phoneNumber);

        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        // Unknown phone, wrong password and unconfirmed phone share one body; the unknown-phone path still pays
        // for a hash so the response time does not tell them apart either.
        if (user is null)
        {
            passwordHasher.HashPassword(request.Password);
            throw InvalidCredentials();
        }

        if (!passwordHasher.VerifyPassword(request.Password, user) || !user.IsPhoneNumberConfirmed)
        {
            throw InvalidCredentials();
        }

        await loginThrottle.ResetAsync(phoneNumber);

        // Deactivated users keep their audit history but cannot authenticate (rule 41). Said only after the
        // password proved who is asking, so it tells a guesser nothing.
        if (!user.IsActive)
        {
            throw AccountDeactivated();
        }

        await refreshTokens.PruneAsync(user.Id);

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = await refreshTokens.IssueAsync(user);

        return new AuthSession(accessToken, refreshToken, user.Language);
    }

    public async Task<AuthSession> RefreshTokenAsync(RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var refreshToken = await context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken)
            ?? throw SessionExpired();

        // Bypasses the organization query filter: refresh is anonymous (no org claim) and the user is fetched by
        // its own id. Without this, a stray org context filters the required User out and token refresh fails.
        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == refreshToken.UserId)
            ?? throw SessionExpired();

        // Rule 41: a deactivated user's session ends here, whatever state this token is in — every token is
        // revoked so no other tab or device can come back either.
        if (!user.IsActive)
        {
            await refreshTokens.RevokeAllAsync(user.Id);
            await context.SaveChangesAsync();

            throw AccountDeactivated();
        }

        if (refreshToken.IsRevoked)
        {
            throw SessionExpired();
        }

        // Single use: the presented token is spent whether it rotates or turns out to be expired.
        refreshToken.IsRevoked = true;
        await context.SaveChangesAsync();

        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw SessionExpired();
        }

        var newAccessToken = tokenService.GenerateAccessToken(user);
        var newRefreshToken = await refreshTokens.IssueAsync(user);

        return new AuthSession(newAccessToken, newRefreshToken, user.Language);
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

    /// <summary>
    /// Phone, email and Telegram are unique across every organization (the indexes are global), so the check looks
    /// past the organization filter. A taken value is a 400 field error, never the unique-index 500.
    /// </summary>
    private async Task EnsureAccountDetailsAvailableAsync(RegisterRequest request)
    {
        var users = context.Users.IgnoreQueryFilters();

        if (await users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber))
        {
            throw CodedValidation.Failure(
                nameof(RegisterRequest.PhoneNumber), "This phone number is already registered.", ErrorCodes.PhoneTaken);
        }

        if (request.Email is not null && await users.AnyAsync(u => u.Email == request.Email))
        {
            throw CodedValidation.Failure(
                nameof(RegisterRequest.Email), "This email is already registered.", ErrorCodes.EmailTaken);
        }

        if (request.TelegramAccount is not null && await users.AnyAsync(u => u.TelegramAccount == request.TelegramAccount))
        {
            throw CodedValidation.Failure(
                nameof(RegisterRequest.TelegramAccount), "This Telegram account is already registered.", ErrorCodes.TelegramTaken);
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AuthenticationFailedException InvalidCredentials() =>
        new(ErrorCodes.InvalidCredentials, "Invalid phone number or password.");

    private static AuthenticationFailedException AccountDeactivated() =>
        new(ErrorCodes.AccountDeactivated, "This account has been deactivated.");

    private static AuthenticationFailedException SessionExpired() =>
        new(ErrorCodes.SessionExpired, "Invalid refresh token");
}
