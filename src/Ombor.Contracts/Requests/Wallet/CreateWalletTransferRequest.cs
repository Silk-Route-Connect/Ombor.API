namespace Ombor.Contracts.Requests.Wallet;

/// <summary>
/// Request to move money between two wallets. Atomic and immutable; hard-blocked when the
/// amount exceeds the source wallet's balance.
/// </summary>
/// <param name="FromWalletId">Source wallet.</param>
/// <param name="ToWalletId">Destination wallet (must differ from the source).</param>
/// <param name="Amount">Amount to move (must be greater than zero).</param>
/// <param name="Note">Optional note.</param>
public sealed record CreateWalletTransferRequest(
    int FromWalletId,
    int ToWalletId,
    decimal Amount,
    string? Note);
