using Ombor.Application.Extensions;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Mappings;

internal static class TemplateMappings
{
    public static TemplateDto ToDto(this Template template)
    {
        if (template.Partner is null)
        {
            throw new InvalidOperationException("Cannot map Template to TemplateDto without Partner.");
        }

        var items = template.Items
            .Select(ToDto)
            .ToArray();

        return new(
            Id: template.Id,
            PartnerId: template.PartnerId,
            PartnerName: template.Partner.Name,
            Name: template.Name,
            Type: template.Type.ToString(),
            Items: items);
    }

    public static CreateTemplateResponse ToCreateResponse(this Template template)
    {
        if (template.Partner is null)
        {
            throw new InvalidOperationException("Cannot map Template to CreateTemplateResponse without Partner.");
        }

        var items = template.Items
            .Select(ToDto)
            .ToArray();

        return new(
            Id: template.Id,
            PartnerId: template.PartnerId,
            PartnerName: template.Partner.Name,
            Name: template.Name,
            Type: template.Type.ToString(),
            Items: items);
    }

    public static UpdateTemplateResponse ToUpdateResponse(this Template template)
    {
        if (template.Partner is null)
        {
            throw new InvalidOperationException("Cannot map Template to UpdateTemplateResponse without Partner.");
        }

        var items = template.Items
            .Select(ToDto)
            .ToArray();

        return new(
            Id: template.Id,
            PartnerId: template.PartnerId,
            PartnerName: template.Partner.Name,
            Name: template.Name,
            Type: template.Type.ToString(),
            Items: items);
    }

    public static void ApplyUpdate(this Template template, UpdateTemplateRequest request, IReadOnlyDictionary<int, int> packageSizes)
    {
        template.Name = request.Name;
        template.PartnerId = request.PartnerId;
        template.Type = request.Type.ToDomain();

        // Reconcile against the original items: update matches, add new ones, drop the rest.
        var existingById = template.Items.Where(i => i.Id != 0).ToDictionary(i => i.Id);
        var keptIds = new HashSet<int>();

        foreach (var requestItem in request.Items)
        {
            // A package-entry item resolves to base units server-side; the package size is snapshotted (rule 21).
            var (quantity, packageSize) = PackageEntry.Resolve(requestItem.ProductId, requestItem.Quantity, requestItem.PackageQuantity, packageSizes);

            if (requestItem.Id != 0 && existingById.TryGetValue(requestItem.Id, out var existing))
            {
                existing.ProductId = requestItem.ProductId;
                existing.Quantity = quantity;
                existing.UnitPrice = requestItem.UnitPrice;
                existing.DiscountAmount = requestItem.Discount;
                existing.DiscountType = requestItem.DiscountType.ToDomainDiscountType();
                existing.PackageSize = packageSize;
                keptIds.Add(existing.Id);
            }
            else
            {
                // A new item — never carry a foreign Id, so EF inserts it.
                template.Items.Add(new TemplateItem
                {
                    ProductId = requestItem.ProductId,
                    Quantity = quantity,
                    UnitPrice = requestItem.UnitPrice,
                    DiscountAmount = requestItem.Discount,
                    DiscountType = requestItem.DiscountType.ToDomainDiscountType(),
                    PackageSize = packageSize,
                    Product = null!,
                    Template = null!,
                });
            }
        }

        foreach (var removed in existingById.Values.Where(i => !keptIds.Contains(i.Id)).ToList())
        {
            template.Items.Remove(removed);
        }
    }

    public static Template ToEntity(this CreateTemplateRequest request, IReadOnlyDictionary<int, int> packageSizes)
    {
        var items = request.Items
            .Select(item => item.ToEntity(packageSizes))
            .ToList();

        return new Template
        {
            Name = request.Name,
            Type = Enum.Parse<TemplateType>(request.Type.ToString()),
            Items = items,
            PartnerId = request.PartnerId,
            Partner = null!, // Will be set by EF
        };
    }

    private static TemplateItem ToEntity(this CreateTemplateItem item, IReadOnlyDictionary<int, int> packageSizes)
    {
        // A package-entry item resolves to base units server-side; the package size is snapshotted (rule 21).
        var (quantity, packageSize) = PackageEntry.Resolve(item.ProductId, item.Quantity, item.PackageQuantity, packageSizes);

        return new()
        {
            ProductId = item.ProductId,
            Quantity = quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.Discount,
            DiscountType = item.DiscountType.ToDomainDiscountType(),
            PackageSize = packageSize,
            Product = null!, // Will be set by EF
            Template = null! // Will be set by EF
        };
    }

    private static TemplateItemDto ToDto(this TemplateItem item)
    {
        if (item.Product is null)
        {
            throw new InvalidOperationException("Cannot map template item without Product.");
        }

        if (item.Template is null)
        {
            throw new InvalidOperationException("Cannot map template item without Template.");
        }

        return new(
            Id: item.Id,
            ProductId: item.ProductId,
            ProductName: item.Product.Name,
            Sku: item.Product.SKU,
            Measurement: item.Product.Measurement.ToString(),
            TemplateId: item.TemplateId,
            TemplateName: item.Template.Name,
            Quantity: item.Quantity,
            UnitPrice: item.UnitPrice,
            Discount: item.DiscountAmount,
            DiscountType: item.DiscountType.ToString(),
            PackageSize: item.PackageSize);
    }

    public static TemplateType ToDomain(this Contracts.Enums.TemplateType type)
        => Enum.Parse<TemplateType>(type.ToString());
}
