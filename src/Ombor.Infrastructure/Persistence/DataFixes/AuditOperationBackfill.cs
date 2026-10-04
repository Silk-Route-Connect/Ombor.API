namespace Ombor.Infrastructure.Persistence.DataFixes;

/// <summary>
/// Gives audit rows recorded before operations existed an operation id and, for child rows, their parent — so the
/// Activity Log groups and filters old and new rows the same way. A legacy operation is one user in one organization
/// within one second (the id is a hash of the three, so a rerun yields the same id). Payment components and
/// allocations and transfer lines get their parent from their own table. Idempotent: it only fills empty columns.
/// Kept as a constant so the migration and its test run the same statement.
/// </summary>
internal static class AuditOperationBackfill
{
    public const string Sql = @"
UPDATE [AuditEntry]
SET [OperationId] = CONVERT(uniqueidentifier, SUBSTRING(HASHBYTES('SHA2_256', CONCAT(
        N'legacy:', [OrganizationId], N':', COALESCE(CONVERT(nvarchar(11), [UserId]), N'-'), N':',
        CONVERT(nchar(19), [TimestampUtc], 120))), 1, 16))
WHERE [OperationId] IS NULL;

UPDATE [a]
SET [ParentEntityType] = N'Payment', [ParentEntityId] = [c].[PaymentId]
FROM [AuditEntry] AS [a]
INNER JOIN [PaymentComponent] AS [c] ON [c].[Id] = [a].[EntityId]
WHERE [a].[EntityType] = N'PaymentComponent' AND [a].[ParentEntityId] IS NULL;

UPDATE [a]
SET [ParentEntityType] = N'Payment', [ParentEntityId] = [p].[PaymentId]
FROM [AuditEntry] AS [a]
INNER JOIN [PaymentAllocation] AS [p] ON [p].[Id] = [a].[EntityId]
WHERE [a].[EntityType] = N'PaymentAllocation' AND [a].[ParentEntityId] IS NULL;

UPDATE [a]
SET [ParentEntityType] = N'Transfer', [ParentEntityId] = [t].[TransferId]
FROM [AuditEntry] AS [a]
INNER JOIN [TransferLine] AS [t] ON [t].[Id] = [a].[EntityId]
WHERE [a].[EntityType] = N'TransferLine' AND [a].[ParentEntityId] IS NULL;";
}
