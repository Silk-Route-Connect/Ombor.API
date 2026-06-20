namespace Ombor.Contracts.Requests.Wallet;

/// <summary>
/// Request to update a wallet. Only the name is editable — type and opening balance are
/// immutable after creation (rule 16).
/// </summary>
/// <param name="Id">The wallet identifier.</param>
/// <param name="Name">The new display name.</param>
public sealed record UpdateWalletRequest(
    int Id,
    string Name);
