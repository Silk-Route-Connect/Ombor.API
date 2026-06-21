using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
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
    IPasswordHasher passwordHasher) : IUserService
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

        var phone = request.Value.Trim();

        if (await context.Users.AnyAsync(u => u.PhoneNumber == phone))
        {
            throw new ValidationException($"A user with phone {phone} already exists.");
        }

        var organizationId = organizationAccessor.OrganizationId
            ?? throw new InvalidOperationException("No organization in the current context.");

        // The invitee gets a usable account with a random password they replace via the OTP / forgot-password flow.
        var password = passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));

        var user = new User
        {
            FirstName = phone,
            LastName = string.Empty,
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

        await context.SaveChangesAsync();

        return Map(user.Id, user.FirstName, user.LastName, user.PhoneNumber, user.IsActive, user.DeactivatedAt);
    }

    public async Task<TenantUserDto> ReactivateAsync(int userId)
    {
        var user = await GetOrThrowAsync(userId);
        user.IsActive = true;
        user.DeactivatedAt = null;

        await context.SaveChangesAsync();

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
