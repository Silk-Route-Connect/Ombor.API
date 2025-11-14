using FluentValidation;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Defines CRUD operations for products.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Retrieves products, optionally filtered by search term, category, or price range.
    /// </summary>
    /// <param name="request">
    /// The filtering options: <see cref="GetProductsRequest.SearchTerm"/>,
    /// <see cref="GetProductsRequest.CategoryId"/>,
    /// <see cref="GetProductsRequest.MinPrice"/>,
    /// <see cref="GetProductsRequest.MaxPrice"/>.
    /// </param>
    /// <returns>A paged list of <see cref="ProductDto"/> matching the criteria.</returns>
    Task<PagedList<ProductDto>> GetAsync(GetProductsRequest request);

    /// <summary>
    /// Retrieves a single product by its identifier.
    /// </summary>
    /// <param name="request">
    /// Contains the <see cref="GetProductByIdRequest.Id"/> of the product to fetch.
    /// </param>
    /// <returns>The matching <see cref="ProductDto"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Product}">If no product with the given ID exists.</exception>
    Task<ProductDto> GetByIdAsync(GetProductByIdRequest request);

    Task<ProductTransactionDto[]> GetTransactionsAsync(GetProductTransactionsRequest request);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">
    /// The details of the product to create, including.
    /// </param>
    /// <returns>Details of the newly created product.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    Task<CreateProductResponse> CreateAsync(CreateProductRequest request);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="request">
    /// Contains the <see cref="UpdateProductRequest.Id"/> and new values for the product.
    /// </param>
    /// <returns>Details of the updated product.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Product}">If no product with the given ID exists.</exception>
    Task<UpdateProductResponse> UpdateAsync(UpdateProductRequest request);

    /// <summary>
    /// Deletes an existing product by its ID.
    /// </summary>
    /// <param name="request">
    /// Contains the <see cref="DeleteProductRequest.Id"/> of the product to remove.
    /// </param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    /// <exception cref="ValidationException">If <paramref name="request"/> fails validation.</exception>
    /// <exception cref="EntityNotFoundException{Product}">If no product with the given ID exists.</exception>
    Task DeleteAsync(DeleteProductRequest request);
}
