namespace Ombor.Contracts.Responses.Payment;

/// <summary>A payment that settled a transaction, derived from its settlement allocation.</summary>
/// <param name="Id">The settlement allocation id.</param>
/// <param name="TransactionId">The transaction settled.</param>
/// <param name="Amount">The amount applied to the transaction.</param>
/// <param name="PaymentNumber">The payment's human-friendly number (e.g. «P-520»).</param>
/// <param name="WalletName">The wallet the money moved through.</param>
/// <param name="WalletType">The wallet type (Cash/Card/Bank).</param>
/// <param name="Notes">Optional payment note.</param>
/// <param name="Date">When the payment was made.</param>
public sealed record TransactionPaymentDto(
    int Id,
    int TransactionId,
    decimal Amount,
    string? PaymentNumber,
    string? WalletName,
    string? WalletType,
    string? Notes,
    DateTimeOffset Date);
