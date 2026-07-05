namespace Ombor.Contracts.Requests.Auth;

/// <summary>Request to set a new password using a verified reset code.</summary>
public sealed record ResetPasswordRequest(string PhoneNumber, string Code, string NewPassword, string ConfirmPassword);
