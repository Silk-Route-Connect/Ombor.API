namespace Ombor.Application.Services.DebtPositions;

/// <summary>
/// The dated events since the earliest cutoff: per partner, what each event added to its balance; per document, the
/// settlements it received. Undoing everything dated at or after a cutoff turns today's figures into that moment's.
/// </summary>
internal sealed class PartnerHistory
{
    public static PartnerHistory Empty { get; } = new();

    private readonly Dictionary<int, List<(DateTimeOffset Date, decimal Amount)>> _effects = [];
    private readonly Dictionary<int, List<(DateTimeOffset Date, decimal Amount)>> _settlements = [];

    public void AddEffect(int partnerId, DateTimeOffset date, decimal amount) => Add(_effects, partnerId, date, amount);

    public void AddSettlement(int transactionId, DateTimeOffset date, decimal amount) => Add(_settlements, transactionId, date, amount);

    /// <summary>The partner's balance change from events dated at or after <paramref name="cutoff"/>.</summary>
    public decimal EffectsFrom(int partnerId, DateTimeOffset cutoff) => SumFrom(_effects, partnerId, cutoff);

    /// <summary>What the document was paid at or after <paramref name="cutoff"/> — still unpaid before it.</summary>
    public decimal SettledFrom(int transactionId, DateTimeOffset cutoff) => SumFrom(_settlements, transactionId, cutoff);

    private static void Add(Dictionary<int, List<(DateTimeOffset, decimal)>> map, int key, DateTimeOffset date, decimal amount)
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }

        list.Add((date, amount));
    }

    private static decimal SumFrom(Dictionary<int, List<(DateTimeOffset Date, decimal Amount)>> map, int key, DateTimeOffset cutoff) =>
        map.TryGetValue(key, out var list) ? list.Where(e => e.Date >= cutoff).Sum(e => e.Amount) : 0m;
}
