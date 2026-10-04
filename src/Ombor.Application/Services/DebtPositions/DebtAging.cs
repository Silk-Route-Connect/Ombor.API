namespace Ombor.Application.Services.DebtPositions;

/// <summary>A dated item that can carry part of a partner's net receivable.</summary>
/// <param name="Amount">What the item still claims (an unpaid document's remaining, a positive opening balance).</param>
/// <param name="Date">When the item dates from.</param>
/// <param name="TransactionId">The document; null for the opening balance.</param>
internal readonly record struct ReceivableItem(decimal Amount, DateTimeOffset Date, int? TransactionId);

/// <summary>How a net receivable is aged and bucketed — one definition for the dashboard and the debts summary.</summary>
internal static class DebtAging
{
    /// <summary>A receivable older than this many days counts as «старше 30 дней».</summary>
    public const int OlderThanDays = 30;

    /// <summary>The age buckets, youngest first.</summary>
    public static readonly string[] Buckets = ["0-7", "8-30", "31-60", "60+"];

    public static int BucketOf(int ageDays) => ageDays switch
    {
        <= 7 => 0,
        <= 30 => 1,
        <= 60 => 2,
        _ => 3,
    };

    /// <summary>
    /// Attributes a partner's net receivable to its receivable items, newest first. Whatever nets the items down —
    /// an advance the partner paid, what we owe them, a negative opening balance — is taken as settling the oldest
    /// items first, the order the settlement auto-allocation uses; so what stays owed sits on the newest items. A
    /// remainder no item explains (a prepayment we made) is counted as current (0 days).
    /// </summary>
    public static IReadOnlyList<AgedPortion> Attribute(
        decimal netReceivable,
        IEnumerable<ReceivableItem> items,
        DateOnly asOf,
        Func<DateTimeOffset, DateOnly> localDateOf)
    {
        if (netReceivable <= 0m)
        {
            return [];
        }

        var portions = new List<AgedPortion>();
        var left = netReceivable;

        foreach (var item in items.OrderByDescending(i => i.Date).ThenByDescending(i => i.TransactionId ?? 0))
        {
            if (left <= 0m)
            {
                break;
            }

            var amount = Math.Min(item.Amount, left);
            var age = Math.Max(0, asOf.DayNumber - localDateOf(item.Date).DayNumber);
            portions.Add(new AgedPortion(amount, age, item.TransactionId));
            left -= amount;
        }

        if (left > 0m)
        {
            portions.Add(new AgedPortion(left, 0, null));
        }

        return portions;
    }

    /// <summary>Organization totals from each partner's net balance and aged receivable.</summary>
    public static DebtTotals Totalize(IEnumerable<(decimal Balance, IReadOnlyList<AgedPortion> Aged)> partners)
    {
        decimal receivable = 0m, payable = 0m, older = 0m;
        int receivablePartners = 0, payablePartners = 0, olderItems = 0, olderPartners = 0;
        var aging = new decimal[Buckets.Length];

        foreach (var (balance, aged) in partners)
        {
            if (balance > 0m)
            {
                receivable += balance;
                receivablePartners++;
            }
            else if (balance < 0m)
            {
                payable -= balance;
                payablePartners++;
            }

            var olderHere = false;
            foreach (var portion in aged)
            {
                aging[BucketOf(portion.AgeDays)] += portion.Amount;

                if (portion.AgeDays > OlderThanDays)
                {
                    older += portion.Amount;
                    olderItems++;
                    olderHere = true;
                }
            }

            if (olderHere)
            {
                olderPartners++;
            }
        }

        return new DebtTotals(receivable, receivablePartners, payable, payablePartners, older, olderItems, olderPartners, aging);
    }
}
