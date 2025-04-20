namespace Ombor.Contracts.Requests.Product;

public sealed record GetProductsRequest(
    string? SearchTerm,
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice);
