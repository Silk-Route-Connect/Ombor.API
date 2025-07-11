﻿using System.Net.Http.Headers;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Requests.Template;

namespace Ombor.Tests.Common.Extensions;

public static class RequestExtensions
{
    public static bool IsEmpty(this GetCategoriesRequest request) =>
        string.IsNullOrWhiteSpace(request.SearchTerm);

    public static bool IsEmpty(this GetProductsRequest request) =>
        string.IsNullOrWhiteSpace(request.SearchTerm) &&
        !request.CategoryId.HasValue &&
        !request.MinPrice.HasValue &&
        !request.MaxPrice.HasValue;

    public static bool IsEmpty(this GetPartnersRequest request) =>
        string.IsNullOrWhiteSpace(request.SearchTerm);

    public static bool IsEmpty(this GetTemplatesRequest request) =>
        string.IsNullOrWhiteSpace(request.SearchTerm) &&
        request.Type.HasValue;

    public static bool IsFullyPopulated(this GetProductsRequest request) =>
        !string.IsNullOrWhiteSpace(request.SearchTerm) &&
        request.CategoryId.HasValue &&
        request.MinPrice.HasValue &&
        request.MaxPrice.HasValue;

    public static MultipartFormDataContent ToMultipartFormData(this CreateProductRequest request)
    {
        var content = new MultipartFormDataContent
        {
            // 1) Non‐file fields
            { new StringContent(request.CategoryId.ToString()), nameof(request.CategoryId) },
            { new StringContent(request.Name), nameof(request.Name) },
            { new StringContent(request.SKU), nameof(request.SKU) },
            { new StringContent(request.SalePrice.ToString()), nameof(request.SalePrice) },
            { new StringContent(request.SupplyPrice.ToString()), nameof(request.SupplyPrice) },
            { new StringContent(request.RetailPrice.ToString()), nameof(request.RetailPrice) },
            { new StringContent(request.QuantityInStock.ToString()), nameof(request.QuantityInStock) },
            { new StringContent(request.LowStockThreshold.ToString()), nameof(request.LowStockThreshold) },
            { new StringContent(((int)request.Measurement).ToString()), nameof(request.Measurement) },
            { new StringContent(((int)request.Type).ToString()), nameof(request.Type) }
        };

        if (request.Description is not null)
            content.Add(new StringContent(request.Description), nameof(request.Description));
        if (request.Barcode is not null)
            content.Add(new StringContent(request.Barcode), nameof(request.Barcode));

        // 2) object types
        if (request.Attachments is null || request.Attachments.Length == 0)
        {
            return content;
        }

        foreach (var file in request.Attachments)
        {
            // open a fresh stream for each
            var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType!);
            content.Add(fileContent, nameof(request.Attachments), file.FileName);
        }

        return content;
    }

    public static MultipartFormDataContent ToMultipartFormData(this UpdateProductRequest request)
    {
        var content = new MultipartFormDataContent
        {
            // 1) Non‐file fields
            { new StringContent(request.Id.ToString()), nameof(request.Id) },
            { new StringContent(request.CategoryId.ToString()), nameof(request.CategoryId) },
            { new StringContent(request.Name), nameof(request.Name) },
            { new StringContent(request.SKU), nameof(request.SKU) },
            { new StringContent(request.SalePrice.ToString()), nameof(request.SalePrice) },
            { new StringContent(request.SupplyPrice.ToString()), nameof(request.SupplyPrice) },
            { new StringContent(request.RetailPrice.ToString()), nameof(request.RetailPrice) },
            { new StringContent(request.QuantityInStock.ToString()), nameof(request.QuantityInStock) },
            { new StringContent(request.LowStockThreshold.ToString()), nameof(request.LowStockThreshold) },
            { new StringContent(((int)request.Measurement).ToString()), nameof(request.Measurement) },
            { new StringContent(((int)request.Type).ToString()), nameof(request.Type) }
        };

        if (request.Description is not null)
            content.Add(new StringContent(request.Description), nameof(request.Description));
        if (request.Barcode is not null)
            content.Add(new StringContent(request.Barcode), nameof(request.Barcode));

        // 2) object types
        if (request.Attachments?.Length > 0)
        {
            foreach (var file in request.Attachments)
            {
                // open a fresh stream for each
                var stream = file.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType!);
                content.Add(fileContent, nameof(request.Attachments), file.FileName);
            }
        }

        if (request.ImagesToDelete?.Length > 0)
        {
            foreach (var imageIdToDelete in request.ImagesToDelete)
            {
                content.Add(new StringContent(imageIdToDelete.ToString()), nameof(request.ImagesToDelete));
            }
        }

        return content;
    }
}
