using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class ProductService(IApplicationDbContext context, IRequestValidator validator) : IProductService
{
    public Task<ProductDto[]> GetAsync(GetProductsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery(request);

        var thresholdDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

        return query
            .AsNoTracking()
            .Select(x => new ProductDto(
                x.Id,
                x.CategoryId,
                x.Category.Name,
                x.Name,
                x.SKU,
                x.Measurement.ToString(),
                x.Description,
                x.Barcode,
                x.SalePrice,
                x.SupplyPrice,
                x.RetailPrice,
                x.QuantityInStock,
                x.LowStockThreshold,
                x.ExpireDate,
                x.QuantityInStock <= x.LowStockThreshold,
                x.ExpireDate >= thresholdDate))
            .ToArrayAsync();
    }

    public async Task<ProductDto> GetByIdAsync(GetProductByIdRequest request)
    {
        validator.ValidateAndThrow(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
    }

    public async Task<CreateProductResponse> CreateAsync(CreateProductRequest request)
    {
        await validator.ValidateAndThrowAsync(request, default);

        var entity = request.ToEntity();
        context.Products.Add(entity);
        await context.SaveChangesAsync();

        var createdProduct = await GetOrThrowAsync(entity.Id);

        return createdProduct.ToCreateResponse();
    }

    public async Task<UpdateProductResponse> UpdateAsync(UpdateProductRequest request)
    {
        await validator.ValidateAndThrowAsync(request, default);

        var entity = await GetOrThrowAsync(request.Id);
        entity.ApplyUpdate(request);
        await context.SaveChangesAsync();

        return entity.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeleteProductRequest request)
    {
        validator.ValidateAndThrow(request);

        var entity = await GetOrThrowAsync(request.Id);
        context.Products.Remove(entity);
        await context.SaveChangesAsync();
    }

    private async Task<Product> GetOrThrowAsync(int id) =>
        await context.Products.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Product>(id);

    private IQueryable<Product> GetQuery(GetProductsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(
                x => x.Name.Contains(request.SearchTerm) ||
                (x.Description != null && x.Description.Contains(request.SearchTerm)) ||
                (x.SKU != null && x.SKU.Contains(request.SearchTerm)) ||
                (x.Barcode != null && x.Barcode.Contains(request.SearchTerm)));
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.SalePrice <= request.MaxPrice.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.SalePrice >= request.MinPrice.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        return query;
    }
}
