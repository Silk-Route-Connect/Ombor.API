namespace Ombor.Domain.Enums;

/// <summary>
/// The document series a per-organization number counter belongs to. Each series numbers
/// independently; transactions share one series across all sub-types (Sale/Supply/refunds).
/// </summary>
public enum NumberSeriesType
{
    Transaction = 1,
    Payment = 2,
    Order = 3,
}
