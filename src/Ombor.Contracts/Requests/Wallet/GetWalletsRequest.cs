namespace Ombor.Contracts.Requests.Wallet;

/// <summary>
/// Request to list wallets. Archived wallets are always included (rule 31).
/// </summary>
/// <param name="SearchTerm">Optional case-insensitive term to filter by name.</param>
public sealed record GetWalletsRequest(string? SearchTerm = null);
