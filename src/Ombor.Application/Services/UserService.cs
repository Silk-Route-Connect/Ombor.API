using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Validators;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.User;
using Ombor.Contracts.Responses.User;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class UserService(
    IApplicationDbContext context,
    IRequestValidator validator,
    ICurrentUserAccessor currentUser,
    IOrganizationAccessor organizationAccessor,
    IPasswordHasher passwordHasher,
    IRefreshTokenStore refreshTokens,
    IActiveUserCache activeUsers,
    ILoginThrottle loginThrottle) : IUserService
{
    public async Task<TenantUserDto[]> GetUsersAsync()
    {
        // Org scoping is automatic via the global query filter; deactivated users are included.
        var users = await context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.PhoneNumber, u.IsActive, u.DeactivatedAt })
            .ToArrayAsync();

        return [.. users.Select(u => Map(u.Id, u.FirstName, u.LastName, u.PhoneNumber, u.IsActive, u.DeactivatedAt))];
    }

    public async Task<TenantUserDto> InviteAsync(InviteUserRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        if (request.Method != ContactType.Phone)
        {
            throw new ValidationException("Only phone invites are supported.");
        }

        var phone = PhoneNumbers.Canonical(request.Value);

        // The phone index is global: a number registered in any organization is taken, not only in this one.
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.PhoneNumber == phone))
        {
            throw CodedValidation.Failure(
                nameof(InviteUserRequest.Value), "This phone number is already registered.", ErrorCodes.PhoneTaken);
        }

        var organizationId = organizationAccessor.OrganizationId
            ?? throw new InvalidOperationException("No organization in the current context.");

        // The invitee's random password is never shown to anyone: they set their own through forgot-password, whose
        // SMS code also confirms the phone (until then login treats the account as unconfirmed).
        var password = passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));

        var user = new User
        {
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? phone : request.FirstName.Trim(),
            LastName = request.LastName?.Trim() ?? string.Empty,
            PhoneNumber = phone,
            PasswordHash = password.Hash,
            PasswordSalt = password.Salt,
            IsPhoneNumberConfirmed = false,
            IsActive = true,
            OrganizationId = organizationId,
            Organization = null!,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return Map(user.Id, user.FirstName, user.LastName, user.PhoneNumber, user.IsActive, user.DeactivatedAt);
    }

    public async Task<TenantUserDto> DeactivateAsync(int userId)
    {
        if (userId == currentUser.UserId)
        {
            throw new ValidationException("You cannot deactivate your own account.");
        }

        var user = await GetOrThrowAsync(userId);
        user.IsActive = false;
        user.DeactivatedAt = DateTimeOffset.UtcNow;

        // Rule 41: deactivation ends every session now — refresh tokens die in this save, and the access-token
        // check stops honouring the user's live access tokens on their next request.
        await refreshTokens.RevokeAllAsync(user.Id);

        await context.SaveChangesAsync();
        await activeUsers.InvalidateAsync(user.Id);

        return Map(user.Id, user.FirstName, user.LastName, user.PhoneNumber, user.IsActive, user.DeactivatedAt);
    }

    public async Task<TenantUserDto> ReactivateAsync(int userId)
    {
        var user = await GetOrThrowAsync(userId);
        user.IsActive = true;
        user.DeactivatedAt = null;

        await context.SaveChangesAsync();
        await activeUsers.InvalidateAsync(user.Id);

        return Map(user.Id, user.FirstName, user.LastName, user.PhoneNumber, user.IsActive, user.DeactivatedAt);
    }

    public async Task SetLanguageAsync(SetLanguageRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("No user in the current context.");

        var user = await GetOrThrowAsync(userId);
        user.Language = request.Language;

        await context.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, string? currentRefreshToken)
    {
        await validator.ValidateAndThrowAsync(request);

        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("No user in the current context.");

        var user = await GetOrThrowAsync(userId);

        // Shares the login lockout: a stolen session must not become an unthrottled password oracle.
        await loginThrottle.BeginAttemptAsync(user.PhoneNumber);

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user))
        {
            throw CodedValidation.Failure(
                nameof(ChangePasswordRequest.CurrentPassword),
                "The current password is incorrect.",
                ErrorCodes.CurrentPasswordInvalid);
        }

        await loginThrottle.ResetAsync(user.PhoneNumber);

        var passwordHash = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = passwordHash.Hash;
        user.PasswordSalt = passwordHash.Salt;

        // Every other session ends with the old password (whoever else holds one is locked out); this device stays
        // signed in.
        await refreshTokens.RevokeAllAsync(user.Id, keepToken: currentRefreshToken);

        await context.SaveChangesAsync();
    }

    private async Task<User> GetOrThrowAsync(int userId) =>
        await context.Users.FirstOrDefaultAsync(u => u.Id == userId)
        ?? throw new EntityNotFoundException<User>(userId);

    private TenantUserDto Map(int id, string firstName, string lastName, string phone, bool isActive, DateTimeOffset? deactivatedAt) =>
        new(
            id,
            $"{firstName} {lastName}".Trim(),
            phone,
            "phone",
            isActive,
            Self: id == currentUser.UserId,
            Online: false,
            LastActiveAt: deactivatedAt);
}
