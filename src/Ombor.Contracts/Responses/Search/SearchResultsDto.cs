using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Responses.Search;

/// <summary>
/// Global-search results for the current organization, one group per record kind. Archived records are included and
/// flagged; each group lists its best matches first (exact, then starting with the query, then containing it; active
/// before archived) up to the requested limit, and counts every match.
/// </summary>
/// <param name="Query">The query as received (trimmed).</param>
/// <param name="Partners">Partners by name, company or phone digits.</param>
/// <param name="Products">Products by name, SKU, barcode or package barcode.</param>
/// <param name="Documents">Sales, supplies, refunds, payments and orders whose number equals the query.</param>
/// <param name="Employees">Employees by name or phone digits.</param>
/// <param name="Warehouses">Warehouses by name.</param>
/// <param name="Wallets">Wallets by name.</param>
public sealed record SearchResultsDto(
    string Query,
    SearchGroupDto Partners,
    SearchGroupDto Products,
    SearchGroupDto Documents,
    SearchGroupDto Employees,
    SearchGroupDto Warehouses,
    SearchGroupDto Wallets);

/// <summary>One group of search results.</summary>
/// <param name="Total">How many records match (the items are the first ones, up to the limit).</param>
/// <param name="Items">The best matches.</param>
public sealed record SearchGroupDto(int Total, SearchHitDto[] Items);

/// <summary>One matching record, with enough to show it and open it.</summary>
/// <param name="EntityKind">
/// The record kind — the Activity Log's vocabulary, which also names the page that opens it: Partner, Product,
/// Employee, Warehouse, Wallet; Sale / Supply / SaleRefund / SupplyRefund (<c>/api/transactions/{id}</c>), Payment /
/// Payroll (<c>/api/payments/{id}</c>), Order (<c>/api/orders/{id}</c>).
/// </param>
/// <param name="Id">The record id.</param>
/// <param name="Label">The name, or for a document its number (bare; the client prepends «№»).</param>
/// <param name="Detail">
/// A second line: the SKU (product), company or first phone (partner), position (employee), location (warehouse), or
/// the partner / employee of a document. Null when there is none.
/// </param>
/// <param name="MatchedOn">Which field matched the query.</param>
/// <param name="IsArchived">Archived (an employee: terminated) — shown, but marked.</param>
/// <param name="Date">Documents only: the document date.</param>
/// <param name="Amount">Documents only: the total (a payment's amount, an order's total).</param>
public sealed record SearchHitDto(
    ActivityEntityKind EntityKind,
    int Id,
    string Label,
    string? Detail,
    SearchMatch MatchedOn,
    bool IsArchived,
    DateTimeOffset? Date = null,
    decimal? Amount = null);
