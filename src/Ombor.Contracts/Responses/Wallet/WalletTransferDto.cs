namespace Ombor.Contracts.Responses.Wallet;

/// <summary>DTO representing an inter-wallet transfer.</summary>
/// <param name="Id">The transfer identifier.</param>
/// <param name="Date">When the transfer occurred.</param>
/// <param name="FromWalletId">Source wallet id.</param>
/// <param name="FromWalletName">Source wallet name.</param>
/// <param name="FromWalletType">Source wallet type.</param>
/// <param name="ToWalletId">Destination wallet id.</param>
/// <param name="ToWalletName">Destination wallet name.</param>
/// <param name="ToWalletType">Destination wallet type.</param>
/// <param name="Amount">Amount moved.</param>
/// <param name="CreatedBy">Who created the transfer.</param>
/// <param name="Note">Optional note.</param>
public sealed record WalletTransferDto(
    int Id,
    DateTimeOffset Date,
    int FromWalletId,
    string FromWalletName,
    string FromWalletType,
    int ToWalletId,
    string ToWalletName,
    string ToWalletType,
    decimal Amount,
    string? CreatedBy,
    string? Note);
