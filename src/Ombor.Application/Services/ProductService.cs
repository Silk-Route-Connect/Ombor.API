using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class ProductService(
    IApplicationDbContext context,
    IRequestValidator validator,
    IFileService fileService,
    IOptions<FileSettings> fileSettings) : IProductService
{
    private readonly FileSettings fileSettings = fileSettings.Value;

    public async Task<PagedList<ProductDto>> GetAsync(GetProductsRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var query = GetQuery(request);
        query = ApplySort(query, request.SortBy);

        var totalCount = await query.CountAsync();

        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var productsDto = products.Select(x => x.ToDto()).ToList();

        return PagedList<ProductDto>.ToPagedList(productsDto, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<ProductDto> GetByIdAsync(GetProductByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
    }

    public async Task<ProductTransactionDto[]> GetTransactionsAsync(GetProductTransactionsRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var transactionLines = await context.TransactionLines
            .AsNoTracking()
            .Where(x => x.ProductId == request.Id)
            .Include(x => x.Product)
            .Include(x => x.Transaction)
            .ThenInclude(t => t.Partner)
            .ToArrayAsync();

        return [.. transactionLines.Select(x => new ProductTransactionDto(
            x.TransactionId,
            x.Transaction.Type.ToString(),
            x.ProductId,
            x.Product.Name,
            x.Transaction.PartnerId,
            x.Transaction.Partner.Name,
            x.Transaction.DateUtc,
            x.Quantity,
            x.Discount,
            x.UnitPrice))];
    }

    public async Task<CreateProductResponse> CreateAsync(CreateProductRequest request)
    {
        await validator.ValidateAndThrowAsync(request, default);

        var entity = request.ToEntity();
        var images = await CreateImages(request.Attachments);
        entity.Images.AddRange(images);

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
        await UpdateImagesAsync(entity, request.Attachments, request.ImagesToDelete);

        await context.SaveChangesAsync();
        entity.Category = await context.Categories.FirstAsync(x => x.Id == request.CategoryId);

        return entity.ToUpdateResponse();
    }

    public async Task DeleteAsync(DeleteProductRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);
        context.Products.Remove(entity);
        var imagesToDelete = entity.Images.Select(x => x.FileName).ToArray();

        if (imagesToDelete.Length > 0)
        {
            await fileService.DeleteAsync(imagesToDelete, fileSettings.ProductUploadsSection);
        }

        await context.SaveChangesAsync();
    }

    private async Task<Product> GetOrThrowAsync(int id) =>
        await context.Products.FirstOrDefaultAsync(x => x.Id == id)
        ?? throw new EntityNotFoundException<Product>(id);

    private IQueryable<Product> GetQuery(GetProductsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Products
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.InventoryItems)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();

            query = query.Where(
                x => x.Name.Contains(searchTerm) ||
                (x.Description != null && x.Description.Contains(searchTerm)) ||
                (x.SKU != null && x.SKU.Contains(searchTerm)) ||
                (x.Barcode != null && x.Barcode.Contains(searchTerm)));
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

        if (request.Type.HasValue)
        {
            var type = request.Type.Value.ToDomain();
            query = query.Where(x => x.Type == type);
        }

        return query;
    }

    private IQueryable<Product> ApplySort(IQueryable<Product> query, string? sortBy)
        => sortBy?.ToLower() switch
        {
            "sku_asc" => query.OrderBy(x => x.SKU),
            "sku_desc" => query.OrderByDescending(x => x.SKU),
            "barcode_asc" => query.OrderBy(x => x.Barcode),
            "barcode_desc" => query.OrderByDescending(x => x.Barcode),
            "saleprice_asc" => query.OrderBy(x => x.SalePrice),
            "saleprice_desc" => query.OrderByDescending(x => x.SalePrice),
            "supplyprice_asc" => query.OrderBy(x => x.SupplyPrice),
            "supplyprice_desc" => query.OrderByDescending(x => x.SupplyPrice),
            "retailprice_asc" => query.OrderBy(x => x.RetailPrice),
            "retailprice_desc" => query.OrderByDescending(x => x.RetailPrice),
            "quantitystock_asc" => query.OrderBy(x => x.QuantityInStock),
            "quantitystock_desc" => query.OrderByDescending(x => x.QuantityInStock),
            "lowstock_asc" => query.OrderBy(x => x.LowStockThreshold),
            "lowstock_desc" => query.OrderByDescending(x => x.LowStockThreshold),
            "type_asc" => query.OrderBy(x => x.Type),
            "type_desc" => query.OrderByDescending(x => x.Type),
            "name_desc" => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

    private async Task UpdateImagesAsync(
        Product entity,
        IEnumerable<IFormFile>? attachments,
        IEnumerable<int>? imageIdsToDelete)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var hasAttachments = attachments?.Any() ?? false;
        var hasImagesToDelete = imageIdsToDelete?.Any() ?? false;

        if (!hasAttachments && !hasImagesToDelete)
        {
            return;
        }

        var imagesToDelete = await DeleteImages(imageIdsToDelete);
        var newImages = await CreateImages(attachments);
        entity.Images = MergeImages(entity.Images, newImages, imagesToDelete);
    }

    private async Task<ProductImage[]> CreateImages(IEnumerable<IFormFile>? attachments)
    {
        if (attachments?.Any() != true)
        {
            return [];
        }

        var fileUrls = await fileService.UploadAsync(attachments, fileSettings.ProductUploadsSection);
        var images = fileUrls
            .Select(file => new ProductImage
            {
                FileName = file.FileName,
                ImageName = file.OriginalFileName,
                OriginalUrl = file.Url,
                ThumbnailUrl = file.ThumbnailUrl,
                Product = null!
            });

        return [.. images];
    }

    private async Task<ProductImage[]> DeleteImages(IEnumerable<int>? imageIdsToDelete)
    {
        if (imageIdsToDelete?.Any() != true)
        {
            return [];
        }

        var images = await context.ProductImages
            .Where(x => imageIdsToDelete.Contains(x.Id))
            .ToArrayAsync();
        var fileNames = images.Select(x => x.FileName).ToArray();

        if (fileNames.Length == 0)
        {
            return [];
        }

        await fileService.DeleteAsync(fileNames, fileSettings.ProductUploadsSection);

        return images;
    }

    private static List<ProductImage> MergeImages(
        IEnumerable<ProductImage> existingImages,
        IEnumerable<ProductImage> newImages,
        IEnumerable<ProductImage> imageIdsToDelete)
    {
        var images = existingImages.ToList();

        foreach (var image in imageIdsToDelete)
        {
            images.RemoveAll(x => x.Id == image.Id);
        }

        return [.. images, .. newImages];
    }
}
