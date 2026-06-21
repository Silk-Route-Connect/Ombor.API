using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.User;

/// <summary>
/// Invites a user to the current organization by a single contact value. v1 supports phone invites only
/// (login is phone-based); the invitee receives a usable account and sets their password via the OTP /
/// forgot-password flow on first sign-in.
/// </summary>
/// <param name="Method">Whether <paramref name="Value"/> is an email or a phone.</param>
/// <param name="Value">The contact value (a phone number in v1).</param>
public sealed record InviteUserRequest(ContactType Method, string Value);
