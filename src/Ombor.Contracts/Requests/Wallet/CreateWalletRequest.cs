using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Requests.Wallet;

/// <summary>
/// Request to create a new wallet.
/// </summary>
/// <param name="Name">The wallet's display name (unique within the organization).</param>
/// <param name="Type">The money location the wallet represents.</param>
/// <param name="OpeningBalance">The starting balance; recorded once as an immutable event.</param>
public sealed record CreateWalletRequest(
    string Name,
    WalletType Type,
    decimal OpeningBalance);
