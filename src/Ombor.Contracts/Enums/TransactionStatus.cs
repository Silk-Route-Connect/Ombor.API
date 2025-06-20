namespace Ombor.Contracts.Enums;

/// <summary>Lifecycle status of a commercial transaction.</summary>
public enum TransactionStatus
{
    /// <summary>Transaction is posted but not fully paid.</summary>
    Open = 1,

    /// <summary>Fully settled – no outstanding balance.</summary>
    Closed = 2
}
