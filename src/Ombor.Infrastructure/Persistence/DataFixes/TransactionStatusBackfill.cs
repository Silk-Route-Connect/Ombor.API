namespace Ombor.Infrastructure.Persistence.DataFixes;

/// <summary>
/// Re-derives every transaction's stored settlement status from its amounts, mirroring
/// <c>TransactionRecord.SettlementStatusOf</c>: nothing paid → Open, part → PartiallyPaid, all → Closed. Seed data
/// once stored part-paid rows as Open (sale №793 read «Не оплачено» with 356 000 of 356 702,01 paid). Idempotent:
/// it only touches rows whose stored value disagrees. Kept as a constant so the migration and its test run the
/// same statement.
/// </summary>
internal static class TransactionStatusBackfill
{
    public const string Sql = @"
UPDATE [TransactionRecord]
SET [Status] = CASE
        WHEN [TotalPaid] <= 0 THEN N'Open'
        WHEN [TotalPaid] < [TotalDue] THEN N'PartiallyPaid'
        ELSE N'Closed'
    END
WHERE [Status] <> CASE
        WHEN [TotalPaid] <= 0 THEN N'Open'
        WHEN [TotalPaid] < [TotalDue] THEN N'PartiallyPaid'
        ELSE N'Closed'
    END;";
}
