namespace Ombor.Contracts.Requests.Search;

/// <summary>A global-search query.</summary>
/// <param name="Q">
/// What to look for, 1–100 characters: part of a name, company, phone, SKU or barcode (case-insensitive, Cyrillic and
/// Latin find each other), or a document number («793», «№793»).
/// </param>
/// <param name="Limit">The most records returned per group, 1–20.</param>
public sealed record SearchRequest(string? Q, int Limit = 5);
