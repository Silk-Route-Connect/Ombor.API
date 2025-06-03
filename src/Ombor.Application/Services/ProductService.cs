using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
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

    public async Task<ProductDto[]> GetAsync(GetProductsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery(request);
        var products = await query
            .OrderBy(x => x.Name)
            .ToArrayAsync();

        return [.. products.Select(x => x.ToDto())];
    }

    public async Task<ProductDto> GetByIdAsync(GetProductByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = await GetOrThrowAsync(request.Id);

        return entity.ToDto();
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

        var query = context.Products.AsNoTracking();

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

        if (request.Type.HasValue)
        {
            var type = request.Type.Value.ToDomain();
            query = query.Where(x => x.Type == type);
        }

        return query;
    }

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
