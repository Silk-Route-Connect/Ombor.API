using System.Globalization;
using System.Net.Http.Headers;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Requests.Transaction;

namespace Ombor.Tests.Common.Extensions;

public static class RequestExtensions
{
    private static readonly CultureInfo cultureInfo = CultureInfo.InvariantCulture;

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

    public static bool IsEmpty(this GetEmployeesRequest request) =>
        string.IsNullOrWhiteSpace(request.SearchTerm);

    public static bool IsEmpty(this GetWarehousesRequest request) =>
        string.IsNullOrWhiteSpace(request.SearchTerm);

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
            { new StringContent(request.SalePrice.ToString(cultureInfo)), nameof(request.SalePrice) },
            { new StringContent(request.SupplyPrice.ToString(cultureInfo)), nameof(request.SupplyPrice) },
            { new StringContent(request.LowStockThreshold.ToString()), nameof(request.LowStockThreshold) },
            { new StringContent(((int)request.Measurement).ToString()), nameof(request.Measurement) },
            { new StringContent(((int)request.Type).ToString()), nameof(request.Type) }
        };

        if (request.Description is not null)
            content.Add(new StringContent(request.Description), nameof(request.Description));
        if (request.Barcode is not null)
            content.Add(new StringContent(request.Barcode), nameof(request.Barcode));

        // 2) object types
        if (request.Packaging is not null)
        {
            content.Add(new StringContent(request.Packaging.Size.ToString()), "Packaging.Size");
            if (request.Packaging.Label is not null)
                content.Add(new StringContent(request.Packaging.Label), "Packaging.Label");
            if (request.Packaging.Barcode is not null)
                content.Add(new StringContent(request.Packaging.Barcode), "Packaging.Barcode");
        }

        if (request.Attachments is null || request.Attachments.Length == 0)
        {
            return content;
        }

        foreach (var file in request.Attachments)
        {
            // open a fresh stream for each
            var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
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
            { new StringContent(request.SalePrice.ToString(cultureInfo)), nameof(request.SalePrice) },
            { new StringContent(request.SupplyPrice.ToString(cultureInfo)), nameof(request.SupplyPrice) },
            { new StringContent(request.LowStockThreshold.ToString()), nameof(request.LowStockThreshold) },
            { new StringContent(((int)request.Measurement).ToString()), nameof(request.Measurement) },
            { new StringContent(((int)request.Type).ToString()), nameof(request.Type) },
        };

        if (request.Description is not null)
            content.Add(new StringContent(request.Description), nameof(request.Description));
        if (request.Barcode is not null)
            content.Add(new StringContent(request.Barcode), nameof(request.Barcode));

        if (request.Packaging is not null)
        {
            content.Add(new StringContent(request.Packaging.Size.ToString()), "Packaging.Size");
            if (request.Packaging.Label is not null)
                content.Add(new StringContent(request.Packaging.Label), "Packaging.Label");
            if (request.Packaging.Barcode is not null)
                content.Add(new StringContent(request.Packaging.Barcode), "Packaging.Barcode");
        }

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

    public static MultipartFormDataContent ToMultipartFormData(this CreateTransactionRequest request)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(request.PartnerId.ToString()),        nameof(request.PartnerId) },
            { new StringContent(((int)request.Type).ToString()),      nameof(request.Type) },
            { new StringContent(request.PaidAmount.ToString(cultureInfo)), nameof(request.PaidAmount) },
            { new StringContent(((int)request.Overpayment).ToString()),   nameof(request.Overpayment) },
        };

        if (request.WarehouseId.HasValue)
            content.Add(new StringContent(request.WarehouseId.Value.ToString()), nameof(request.WarehouseId));

        if (request.WalletId.HasValue)
            content.Add(new StringContent(request.WalletId.Value.ToString()), nameof(request.WalletId));

        if (request.OriginalTransactionId.HasValue)
            content.Add(new StringContent(request.OriginalTransactionId.Value.ToString()), nameof(request.OriginalTransactionId));

        if (!string.IsNullOrEmpty(request.RefundReason))
            content.Add(new StringContent(request.RefundReason), nameof(request.RefundReason));

        if (request.DueDate.HasValue)
            content.Add(new StringContent(request.DueDate.Value.ToString("yyyy-MM-dd", cultureInfo)), nameof(request.DueDate));

        // Lines
        for (var i = 0; i < request.Lines.Length; i++)
        {
            var l = request.Lines[i];
            content.Add(new StringContent(l.ProductId.ToString()), $"Lines[{i}].ProductId");
            content.Add(new StringContent(l.UnitPrice.ToString(cultureInfo)), $"Lines[{i}].UnitPrice");
            content.Add(new StringContent(l.Discount.ToString(cultureInfo)), $"Lines[{i}].Discount");
            content.Add(new StringContent(((int)l.DiscountType).ToString()), $"Lines[{i}].DiscountType");
            content.Add(new StringContent(l.Quantity.ToString(cultureInfo)), $"Lines[{i}].Quantity");
        }

        // Settlements of other open transactions (optional)
        if (request.Settlements is not null)
        {
            for (var i = 0; i < request.Settlements.Length; i++)
            {
                var s = request.Settlements[i];
                content.Add(new StringContent(s.TransactionId.ToString()), $"Settlements[{i}].TransactionId");
                content.Add(new StringContent(s.Amount.ToString(cultureInfo)), $"Settlements[{i}].Amount");
            }
        }

        if (!string.IsNullOrEmpty(request.Notes))
            content.Add(new StringContent(request.Notes), nameof(request.Notes));

        // Attachments
        if (request.Attachments is not null)
        {
            foreach (var f in request.Attachments)
            {
                var s = f.OpenReadStream();
                var fc = new StreamContent(s);
                fc.Headers.ContentType = MediaTypeHeaderValue.Parse(f.ContentType ?? "application/octet-stream");
                content.Add(fc, nameof(request.Attachments), f.FileName);
            }
        }

        return content;
    }
}
