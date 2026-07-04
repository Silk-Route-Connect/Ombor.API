namespace Ombor.Contracts.Enums;

public enum TransactionStatus
{
    Open = 1,
    Closed = 2,
    PartiallyPaid = 3,

    /// <summary>
    /// A non-closed transaction whose due date has passed. Computed on read from the due date (rule 2),
    /// never stored — so it is a served/queryable status but is never assigned to a persisted row.
    /// </summary>
    Overdue = 4,
}
