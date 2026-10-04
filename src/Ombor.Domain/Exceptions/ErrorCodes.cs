namespace Ombor.Domain.Exceptions;

/// <summary>
/// Machine-readable error codes served as the <c>code</c> extension member of every error ProblemDetails, so a
/// client localizes the message by code and never shows raw server text. Codes are <c>area.reason</c>; optional
/// values the client interpolates travel in the <c>params</c> extension (camelCase keys, listed per code).
/// The agreed list lives in <c>Ombor.Docs/backend-contracts/conventions.md</c> — add a code there in the same change.
/// </summary>
public static class ErrorCodes
{
    /// <summary>Unknown phone, wrong password, or unconfirmed phone at login — one uniform 401.</summary>
    public const string InvalidCredentials = "auth.invalid_credentials";

    /// <summary>The phone number already belongs to an account (400, field error on the phone).</summary>
    public const string PhoneTaken = "auth.phone_taken";

    /// <summary>The email already belongs to an account (400, field error on the email).</summary>
    public const string EmailTaken = "auth.email_taken";

    /// <summary>The Telegram account already belongs to an account (400, field error on the Telegram account).</summary>
    public const string TelegramTaken = "auth.telegram_taken";

    /// <summary>A one-time code does not match.</summary>
    public const string CodeInvalid = "auth.code_invalid";

    /// <summary>A one-time code expired or was never issued — request a new one.</summary>
    public const string CodeExpired = "auth.code_expired";

    /// <summary>Too many wrong one-time codes: the code was invalidated — request a new one.</summary>
    public const string TooManyAttempts = "auth.too_many_attempts";

    /// <summary>Throttled (429). Params: <c>retryAfterSeconds</c>.</summary>
    public const string RateLimited = "auth.rate_limited";

    /// <summary>The account is deactivated (rule 41) — only reported after a correct password or on a live session.</summary>
    public const string AccountDeactivated = "auth.account_deactivated";

    /// <summary>The refresh token is missing, unknown, revoked or expired — sign in again.</summary>
    public const string SessionExpired = "auth.session_expired";

    /// <summary>The current password given to change-password is wrong (400, field error on the current password).</summary>
    public const string CurrentPasswordInvalid = "auth.current_password_invalid";

    /// <summary>Negative stock is impossible (400). Params: <c>productName</c>, <c>available</c>, <c>requested</c>.</summary>
    public const string StockInsufficient = "stock.insufficient";

    /// <summary>A wallet cannot be overdrawn (400). Params: <c>walletName</c>, <c>available</c>, <c>requested</c>.</summary>
    public const string WalletInsufficientBalance = "wallet.insufficient_balance";

    /// <summary>
    /// A payment settles a transaction of the opposite direction (400, field error on the settlement): Income settles
    /// only Sale/SupplyRefund, Expense only Supply/SaleRefund.
    /// </summary>
    public const string PaymentDirectionMismatch = "payment.direction_mismatch";

    /// <summary>
    /// The product SKU is already used by another product of the organization, archived ones included (400, field
    /// error on <c>SKU</c>).
    /// </summary>
    public const string ProductSkuTaken = "product.sku_taken";

    /// <summary>A referenced record cannot be deleted (409) — archive it instead.</summary>
    public const string EntityReferenced = "entity.referenced";

    /// <summary>The value must be unique and is already taken (409) — the database's unique-index safety net.</summary>
    public const string ConflictDuplicate = "conflict.duplicate";

    /// <summary>
    /// Another money or stock write of the organization held its write lock for longer than the timeout (409). Nothing
    /// was recorded; the same request can be sent again.
    /// </summary>
    public const string ConflictBusy = "conflict.busy";

    /// <summary>
    /// An upload was rejected: empty, not an allowed type for this upload (images only for product images and logos),
    /// or its content does not match its extension (400).
    /// </summary>
    public const string FileInvalid = "file.invalid";

    /// <summary>An upload exceeds the size limit (400). Params: <c>maxMegabytes</c>.</summary>
    public const string FileTooLarge = "file.too_large";

    /// <summary>The record does not exist (404).</summary>
    public const string EntityNotFound = "entity.not_found";

    /// <summary>Generic 400 with field errors.</summary>
    public const string ValidationFailed = "validation.failed";

    /// <summary>
    /// Whether <paramref name="code"/> is one of these domain codes rather than a FluentValidation built-in
    /// (e.g. <c>NotEmptyValidator</c>). Domain codes always contain a dot.
    /// </summary>
    public static bool IsDomainCode(string? code) =>
        !string.IsNullOrEmpty(code) && code.Contains('.', StringComparison.Ordinal);
}
