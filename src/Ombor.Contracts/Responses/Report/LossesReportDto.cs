namespace Ombor.Contracts.Responses.Report;

/// <summary>
/// Stock written off by decrease adjustments in a period — the loss line kept apart from cost of goods sold (rule 24) —
/// valued at the cost snapshotted on each adjustment.
/// </summary>
/// <param name="From">First local calendar day of the period, inclusive.</param>
/// <param name="To">Last local calendar day of the period, inclusive.</param>
/// <param name="Value">Everything written off (Σ adjustment value, the figure the adjustments list serves).</param>
/// <param name="Count">Decrease adjustments.</param>
/// <param name="Products">By product, largest value first.</param>
/// <param name="Reasons">By reason, largest value first.</param>
public sealed record LossesReportDto(
    DateOnly From,
    DateOnly To,
    decimal Value,
    int Count,
    LossProductDto[] Products,
    LossReasonDto[] Reasons);

/// <summary>One product's write-offs.</summary>
/// <param name="ProductId">The product id.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Sku">The product SKU.</param>
/// <param name="Measurement">The product's unit of measurement.</param>
/// <param name="Quantity">Units written off (base units).</param>
/// <param name="Value">Their cost.</param>
/// <param name="Count">Adjustments.</param>
public sealed record LossProductDto(
    int ProductId,
    string ProductName,
    string Sku,
    string Measurement,
    decimal Quantity,
    decimal Value,
    int Count);

/// <summary>Write-offs for one reason.</summary>
/// <param name="Reason">The decrease reason code (<c>Damage</c>, <c>Expiry</c>, <c>Theft</c>, <c>RecountDown</c>, <c>Other</c>).</param>
/// <param name="Value">Their cost.</param>
/// <param name="Count">Adjustments.</param>
public sealed record LossReasonDto(
    string Reason,
    decimal Value,
    int Count);
