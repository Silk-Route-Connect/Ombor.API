namespace Ombor.Domain.Enums;

/// <summary>
/// Where a payment component's money comes from (rule 9). A <see cref="Wallet"/> source moves
/// real cash through a wallet; an <see cref="Advance"/> source draws against the partner's
/// advance claim and references no wallet.
/// </summary>
public enum PaymentSourceType
{
    Wallet = 1,
    Advance = 2,
}
