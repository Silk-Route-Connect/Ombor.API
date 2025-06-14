using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Mappings;

internal static class TemplateMappings
{
    public static TemplateDto ToDto(this Template template)
    {
        var items = template.Items
            .Select(ToDto)
            .ToArray();

        return new(
            Id: template.Id,
            Name: template.Name,
            Type: template.Type.ToString(),
            Items: items);
    }

    public static CreateTemplateResponse ToCreateResponse(this Template template)
    {
        var items = template.Items
            .Select(ToDto)
            .ToArray();

        return new(
            Id: template.Id,
            Name: template.Name,
            Type: template.Type.ToString(),
            Items: items);
    }

    public static UpdateTemplateResponse ToUpdateResponse(this Template template)
    {
        var items = template.Items
            .Select(ToDto)
            .ToArray();

        return new(
            Id: template.Id,
            Name: template.Name,
            Type: template.Type.ToString(),
            Items: items);
    }

    public static void ApplyUpdate(this Template template, UpdateTemplateRequest request)
    {
        var items = request.Items
            .Select(ToEntity)
            .ToList();

        template.Name = request.Name;
        template.Type = request.Type.ToDomain();
        template.Items = items;
    }

    public static Template ToEntity(this CreateTemplateRequest request)
    {
        var items = request.Items
            .Select(ToEntity)
            .ToList();

        return new Template
        {
            Name = request.Name,
            Type = Enum.Parse<TemplateType>(request.Type.ToString()),
            Items = items
        };
    }

    private static TemplateItem ToEntity(this CreateTemplateItem item)
        => new()
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.Discount,
            Product = null!, // Will be set by EF!
            Template = null! // Will be set by EF!
        };

    private static TemplateItem ToEntity(this UpdateTemplateItem item)
        => new()
        {
            Id = item.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.Discount,
            Product = null!, // Will be set by EF!
            Template = null! // Will be set by EF!
        };

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
            TemplateId: item.TemplateId,
            TemplateName: item.Template.Name,
            Quantity: item.Quantity,
            UnitPrice: item.UnitPrice,
            Discount: item.DiscountAmount);
    }

    public static TemplateType ToDomain(this Contracts.Enums.TemplateType type)
        => Enum.Parse<TemplateType>(type.ToString());
}
