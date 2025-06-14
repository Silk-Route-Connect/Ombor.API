using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Template;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Factories;

public static class TemplateRequestFactory
{
    public static CreateTemplateRequest GenerateValidCreateRequest() =>
        new(Name: "Test Template To Create",
            Type: TemplateType.Supply,
            Items: GetValidCreateItems());

    public static CreateTemplateRequest GenerateInvalidCreateRequest() =>
        new(Name: "", // Invalid name
            Type: TemplateType.Supply,
            Items: GetInvalidCreateItems());

    public static UpdateTemplateRequest GenerateValidUpdateRequest(int templateId, IEnumerable<TemplateItem> existingItems) =>
        new(Id: templateId,
            Name: "Test Template To Update",
            Type: TemplateType.Sale,
            Items: GetValidUpdateItems(existingItems));

    public static UpdateTemplateRequest GenerateInvalidUpdateRequest(int templateId) =>
        new(Id: templateId,
            Name: "",
            Type: TemplateType.Supply,
            Items: GetInvalidUpdateItems());

    private static CreateTemplateItem[] GetValidCreateItems() =>
        Enumerable.Range(1, 5)
        .Select(i => new CreateTemplateItem(
            ProductId: i,
            Quantity: i + 1,
            UnitPrice: i * 1000,
            Discount: 0))
        .ToArray();

    private static CreateTemplateItem[] GetInvalidCreateItems() =>
        Enumerable.Range(1, 5)
        .Select(i => new CreateTemplateItem(
            ProductId: i,
            Quantity: -5, // Invalid quantity
            UnitPrice: i * -1000, // Invalid unit price
            Discount: -50)) // Invalid discount
        .ToArray();

    private static UpdateTemplateItem[] GetValidUpdateItems(IEnumerable<TemplateItem> existingItems)
    {
        var updatedExistingItems = existingItems.Select(
            item => new UpdateTemplateItem(
                Id: item.Id,
                ProductId: item.ProductId,
                Quantity: item.Quantity + 10,
                UnitPrice: item.UnitPrice + 1_000,
                Discount: item.DiscountAmount + 50));
        var newTemplateItems = Enumerable.Range(1, 5)
            .Select(i => new UpdateTemplateItem(
                Id: 0, // Should trigger creation
                ProductId: i, // Assuming that at least 10 test products werer generated
                Quantity: i + 5,
                UnitPrice: i + 1_000,
                Discount: 0));

        return [.. updatedExistingItems, .. newTemplateItems];
    }

    private static UpdateTemplateItem[] GetInvalidUpdateItems() =>
        Enumerable.Range(1, 5)
        .Select(i => new UpdateTemplateItem(
            Id: 0,
            ProductId: i,
            Quantity: -i, // Invalid quantity
            UnitPrice: i * -1000, // Invalid unit price
            Discount: i * -50)) // Invalid discount
        .ToArray();
}
