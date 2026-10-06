using Microsoft.EntityFrameworkCore;
using Ombor.Application.Configurations;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Localization;
using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class PasswordResetService(
    IApplicationDbContext context,
    ISmsQueue smsQueue,
    IOtpCodeProvider otpCodeProvider,
    IPasswordHasher passwordHasher,
    IRequestValidator validator,
    IRefreshTokenStore refreshTokens,
    ILoginThrottle loginThrottle) : IPasswordResetService
{
    private const int ResetCodeLifetimeMinutes = OtpSettings.CodeLifetimeMinutes;
    private const string InvalidCodeMessage = "The reset code is invalid or has expired.";
    private const string TooManyAttemptsMessage = "Too many wrong codes. Request a new code.";

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var phoneNumber = PhoneNumbers.Canonical(request.PhoneNumber);

        // The send budget is spent whether or not the number has an account, so throttling reveals nothing either.
        await otpCodeProvider.EnsureCanSendAsync(phoneNumber, OtpPurpose.PasswordReset);

        // Generic acknowledgement whether or not the number has an account, so the response never reveals which
        // phone numbers are registered (owner decision). A code is issued either way and only sent to an account, so
        // verify-reset-code answers an unknown number exactly like a wrong guess (auth.code_invalid, the same attempt
        // count, the same expiry) instead of «expired». The SMS is queued, not awaited: response time and status (no
        // provider 503) must not depend on whether an account exists.
        var code = await otpCodeProvider.GenerateOtpAsync(phoneNumber, OtpPurpose.PasswordReset);

        // The account's interface language picks the SMS text; no row means no account, and nothing is sent.
        var language = await context.Users.IgnoreQueryFilters()
            .Where(u => u.PhoneNumber == phoneNumber)
            .Select(u => u.Language)
            .FirstOrDefaultAsync();

        if (language is not null)
        {
            smsQueue.Enqueue(new SmsMessage(phoneNumber, SmsTexts.PasswordReset(code, language), SmsTexts.Subject));
        }

        return new ForgotPasswordResponse(
            "If an account exists for this number, a reset code has been sent.",
            ResetCodeLifetimeMinutes,
            otpCodeProvider.CodeLength,
            otpCodeProvider.ResendAfterSeconds);
    }

    public async Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var phoneNumber = PhoneNumbers.Canonical(request.PhoneNumber);
        var check = await otpCodeProvider.VerifyOtpAsync(phoneNumber, OtpPurpose.PasswordReset, request.Code);

        return check == OtpCheckResult.Valid
            ? new VerifyResetCodeResponse(true)
            : new VerifyResetCodeResponse(false, FailureMessage(check), check.ToErrorCode());
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var phoneNumber = PhoneNumbers.Canonical(request.PhoneNumber);
        var check = await otpCodeProvider.VerifyOtpAsync(phoneNumber, OtpPurpose.PasswordReset, request.Code);

        if (check != OtpCheckResult.Valid)
        {
            return new ResetPasswordResponse(false, FailureMessage(check), check.ToErrorCode());
        }

        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        if (user is null)
        {
            // The code of a number without an account is never sent, so a match here is a lucky guess: same answer as
            // a wrong code.
            return new ResetPasswordResponse(false, InvalidCodeMessage, OtpCheckResult.Invalid.ToErrorCode());
        }

        var passwordHash = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = passwordHash.Hash;
        user.PasswordSalt = passwordHash.Salt;

        // A valid SMS code proves the caller owns the phone. This is also how an invited user (created unconfirmed)
        // completes the first sign-in: invite → forgot password → reset → login.
        user.IsPhoneNumberConfirmed = true;

        // Force a fresh login everywhere after a password change (owner decision): a session opened before the
        // reset — including one an attacker may hold — must not survive it.
        await refreshTokens.RevokeAllAsync(user.Id);

        await context.SaveChangesAsync();
        await otpCodeProvider.RemoveOtpAsync(phoneNumber, OtpPurpose.PasswordReset);

        // The owner just proved possession of the phone: a lockout caused by someone else's guesses must not keep
        // them out.
        await loginThrottle.ResetAsync(phoneNumber);

        return new ResetPasswordResponse(true, "Your password has been reset.");
    }

    private static string FailureMessage(OtpCheckResult check) =>
        check == OtpCheckResult.TooManyAttempts ? TooManyAttemptsMessage : InvalidCodeMessage;
}
