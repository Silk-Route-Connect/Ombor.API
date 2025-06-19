namespace Ombor.Contracts.Enums;

/// <summary>
/// Enumeration of transaction statuses.
/// </summary>
public enum TransactionStatus
{
    /// <summary> Transaction is unpaid or partially paid.</summary>
    Open = 1,

    /// <summary> Transaction is fully paid.</summary>
    Closed = 2,
}
