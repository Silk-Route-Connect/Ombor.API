namespace Ombor.Contracts.Responses.Wallet;

/// <summary>
/// DTO representing a wallet for client consumption. All money figures are server-computed.
/// </summary>
/// <param name="Id">The wallet identifier.</param>
/// <param name="Name">The wallet name.</param>
/// <param name="Type">The money location (Cash / Card / Bank).</param>
/// <param name="Balance">Computed: opening balance + wallet-sourced payment components + transfers (rule 15).</param>
/// <param name="AdvancesHeld">Computed: partner advances physically held in this wallet (rule 11).</param>
/// <param name="OurMoney">Computed: <paramref name="Balance"/> − <paramref name="AdvancesHeld"/> (rule 12).</param>
/// <param name="OpeningBalance">The immutable opening-balance event amount.</param>
/// <param name="IsArchived">Whether the wallet is archived.</param>
/// <param name="CreatedBy">Who created the wallet.</param>
/// <param name="CreatedAt">When the wallet was created.</param>
/// <param name="IsDeletable">Whether the wallet can be hard-deleted (false once a payment or transfer references it); otherwise DELETE returns 409.</param>
public sealed record WalletDto(
    int Id,
    string Name,
    string Type,
    decimal Balance,
    decimal AdvancesHeld,
    decimal OurMoney,
    decimal OpeningBalance,
    bool IsArchived,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    bool IsDeletable = false);
