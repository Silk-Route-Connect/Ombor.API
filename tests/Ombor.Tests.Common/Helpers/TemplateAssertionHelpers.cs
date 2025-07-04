using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

public static class TemplateAssertionHelpers
{
    public static void AssertEquivalent(Template[] expected, TemplateDto[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);

        for (int i = 0; i < expected.Length; i++)
        {
            var expectedTemplate = expected[i];
            var actualTemplate = actual[i];

            AssertEquivalent(expectedTemplate, actualTemplate);
        }
    }

    public static void AssertEquivalent(Template? expected, TemplateDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expected.Partner.Name, actual.PartnerName);
        AssertEquivalent([.. expected.Items], actual.Items);
    }

    public static void AssertEquivalent(CreateTemplateRequest expected, CreateTemplateResponse actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        Assert.Equal(expected.Items.Length, actual.Items.Length);
    }

    public static void AssertEquivalent(CreateTemplateRequest expected, Template actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type.ToString(), actual.Type.ToString());
        Assert.Equal(expected.Items.Length, actual.Items.Count);
    }

    public static void AssertEquivalent(CreateTemplateResponse expected, Template actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expected.PartnerName, actual.Partner.Name);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type, actual.Type.ToString());
        Assert.Equal(expected.Items.Length, actual.Items.Count);
    }

    public static void AssertEquivalent(UpdateTemplateRequest expected, UpdateTemplateResponse actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        AssertEquivalent(expected.Items, actual.Items);
    }

    public static void AssertEquivalent(UpdateTemplateRequest expected, Template? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type.ToString(), actual.Type.ToString());
        AssertEquivalent(expected.Items, [.. actual.Items]);
    }

    public static void AssertEquivalent(UpdateTemplateResponse expected, Template? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.PartnerId, actual.PartnerId);
        Assert.Equal(expected.PartnerName, actual.Partner.Name);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type, actual.Type.ToString());
        AssertEquivalent(expected.Items, [.. actual.Items]);
    }

    private static void AssertEquivalent(TemplateItem[] expected, TemplateItemDto[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);

        for (int i = 0; i < expected.Length; i++)
        {
            var expectedItem = expected[i];
            var actualItem = actual[i];

            AssertEquivalent(expectedItem, actualItem);
        }
    }

    private static void AssertEquivalent(TemplateItem expected, TemplateItemDto actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Product.Name, actual.ProductName);
        Assert.Equal(expected.ProductId, actual.ProductId);
        Assert.Equal(expected.Template.Name, actual.TemplateName);
        Assert.Equal(expected.TemplateId, actual.TemplateId);
        Assert.Equal(expected.Quantity, actual.Quantity);
        Assert.Equal(expected.UnitPrice, actual.UnitPrice);
        Assert.Equal(expected.DiscountAmount, actual.Discount);
    }

    private static void AssertEquivalent(UpdateTemplateItem[] expected, TemplateItemDto[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        // TODO: Assert each item
    }

    private static void AssertEquivalent(UpdateTemplateItem[] expected, TemplateItem[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        // TODO: Assert each item
    }

    private static void AssertEquivalent(TemplateItemDto[] expected, TemplateItem[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        // TODO: Assert each item
    }
}
