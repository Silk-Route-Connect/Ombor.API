namespace Ombor.Application.Services.Reports;

/// <summary>
/// The figures of a set of document lines — documents and their refunds counted apart, quantities and amounts net of
/// refunds. One rule for the sales, purchases and profit reports and the dashboard's gross profit.
/// </summary>
internal readonly record struct LineFigures(
    int Documents,
    int RefundDocuments,
    decimal Quantity,
    decimal Amount,
    decimal RefundAmount,
    decimal Cost,
    bool CostIsEstimated)
{
    public decimal NetAmount => Amount - RefundAmount;

    public decimal GrossProfit => NetAmount - Cost;

    /// <summary>Gross margin on net revenue, 2 decimals; null unless net revenue is positive.</summary>
    public decimal? MarginPercent =>
        NetAmount > 0m ? Math.Round(GrossProfit / NetAmount * 100m, 2, MidpointRounding.AwayFromZero) : null;

    public static LineFigures Of(IReadOnlyCollection<ReportLine> lines)
    {
        decimal quantity = 0m, amount = 0m, refundAmount = 0m, cost = 0m;
        var documents = new HashSet<int>();
        var refundDocuments = new HashSet<int>();

        foreach (var line in lines)
        {
            var sign = line.IsRefund ? -1m : 1m;
            quantity += sign * line.Quantity;
            cost += sign * line.Cost;

            if (line.IsRefund)
            {
                refundAmount += line.Amount;
                refundDocuments.Add(line.TransactionId);
            }
            else
            {
                amount += line.Amount;
                documents.Add(line.TransactionId);
            }
        }

        return new LineFigures(
            documents.Count,
            refundDocuments.Count,
            quantity,
            Money(amount),
            Money(refundAmount),
            Money(cost),
            lines.Any(l => l.CostIsEstimated));
    }

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
