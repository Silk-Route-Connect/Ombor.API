using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.User;

/// <summary>
/// Invites a user to the current organization by a single contact value. v1 supports phone invites only
/// (login is phone-based). The invitee gets an account with an unusable random password and completes the first
/// sign-in through «forgot password»: the SMS code proves the phone and sets their password.
/// </summary>
/// <param name="Method">Whether <paramref name="Value"/> is an email or a phone.</param>
/// <param name="Value">The contact value — a Uzbek phone number in v1 (stored as +998XXXXXXXXX).</param>
/// <param name="FirstName">Optional first name; the phone number is shown when omitted.</param>
/// <param name="LastName">Optional last name.</param>
public sealed record InviteUserRequest(ContactType Method, string Value, string? FirstName = null, string? LastName = null);
