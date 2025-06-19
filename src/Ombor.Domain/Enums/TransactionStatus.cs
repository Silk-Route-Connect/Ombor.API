namespace Ombor.Domain.Enums;

/// <summary>
/// Enumeration of transaction statuses.
/// </summary>
public enum TransactionStatus
{
    /// <summary>The status of the transaction is not known. Should not be persisted.</summary>
    Unkown = 0,

    /// <summary> Transaction is unpaid or partially paid.</summary>
    Open = 1,

    /// <summary> Transaction is fully paid.</summary>
    Closed = 2,
}
