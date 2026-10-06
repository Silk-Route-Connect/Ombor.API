namespace Ombor.Contracts.Requests.User;

/// <summary>Changes the signed-in user's password.</summary>
/// <param name="CurrentPassword">The password in use now (re-verified).</param>
/// <param name="NewPassword">The new password (8–250 characters).</param>
/// <param name="ConfirmPassword">Must equal <paramref name="NewPassword"/>.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
