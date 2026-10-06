namespace Ombor.Infrastructure.Persistence.DataFixes;

/// <summary>
/// Gives Sale and SaleRefund lines recorded before cost snapshots existed (scope-9) the best available cost estimate and
/// marks it estimated, so profit for older periods can be shown with a warning instead of not at all. A sale line takes
/// the current WAC of its product in the sale's warehouse, else the product's organization-wide average (value-weighted
/// over stocked rows, else the plain average), else its supply price, else 0. A refund line takes its original sale's
/// cost of the product — so a refund cancels exactly the cost its sale booked — else the same estimate. Idempotent: it
/// only fills lines without a cost, and every line it reaches gets one. Kept as a constant so the migration and its test
/// run the same statement.
/// </summary>
internal static class TransactionLineCostBackfill
{
    private const string Estimate = @"
OUTER APPLY (
    SELECT TOP (1) i.[AverageCost]
    FROM [WarehouseItem] i
    WHERE i.[WarehouseId] = t.[WarehouseId] AND i.[ProductId] = l.[ProductId] AND i.[AverageCost] > 0
) here
OUTER APPLY (
    SELECT
        SUM(CASE WHEN i.[Quantity] > 0 THEN i.[Quantity] * i.[AverageCost] END)
            / NULLIF(SUM(CASE WHEN i.[Quantity] > 0 THEN i.[Quantity] END), 0) AS [Weighted],
        AVG(i.[AverageCost]) AS [Plain]
    FROM [WarehouseItem] i
    WHERE i.[ProductId] = l.[ProductId] AND i.[AverageCost] > 0
) org
JOIN [Product] p ON p.[Id] = l.[ProductId]";

    private const string EstimatedCost =
        "here.[AverageCost], org.[Weighted], org.[Plain], NULLIF(p.[SupplyPrice], 0), 0";

    public const string Sql = @"
UPDATE l
SET l.[UnitCost] = ROUND(COALESCE(" + EstimatedCost + @"), 2),
    l.[CostIsEstimated] = 1
FROM [TransactionLine] l
JOIN [TransactionRecord] t ON t.[Id] = l.[TransactionId]" + Estimate + @"
WHERE t.[Type] = N'Sale' AND l.[UnitCost] IS NULL;

UPDATE l
SET l.[UnitCost] = ROUND(COALESCE(sold.[Cost], " + EstimatedCost + @"), 2),
    l.[CostIsEstimated] = 1
FROM [TransactionLine] l
JOIN [TransactionRecord] t ON t.[Id] = l.[TransactionId]
OUTER APPLY (
    SELECT SUM(o.[UnitCost] * o.[Quantity]) / NULLIF(SUM(o.[Quantity]), 0) AS [Cost]
    FROM [TransactionLine] o
    WHERE o.[TransactionId] = t.[OriginalTransactionId] AND o.[ProductId] = l.[ProductId] AND o.[UnitCost] IS NOT NULL
) sold" + Estimate + @"
WHERE t.[Type] = N'SaleRefund' AND l.[UnitCost] IS NULL;";
}
