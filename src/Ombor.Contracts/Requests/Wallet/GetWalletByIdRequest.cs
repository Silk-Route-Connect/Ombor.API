namespace Ombor.Contracts.Requests.Wallet;

/// <summary>Request to fetch a single wallet by id.</summary>
/// <param name="Id">The wallet identifier.</param>
public sealed record GetWalletByIdRequest(int Id);
