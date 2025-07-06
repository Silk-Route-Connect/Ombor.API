using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Template;
using Ombor.Domain.Entities;
using ContractType = Ombor.Contracts.Enums.TemplateType;
using DomainType = Ombor.Domain.Enums.TemplateType;

namespace Ombor.Tests.Unit.Mappings;

public sealed class TemplateMappingsTests
{
    [Fact]
    public void ToDto_ShouldThrowInvalidOperationException_WhenPartnerIsNull()
    {
        // Arrange
        var template = new Template
        {
            Id = 42,
            PartnerId = 99,
            Partner = null!,
            Name = "Invalid",
            Type = DomainType.Sale,
            Items = []
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToDto());
    }

    [Fact]
    public void ToDto_ShouldMapAllFieldsCorrectly_WhenTemplateHasPartnerAndItems()
    {
        // Arrange
        var partner = new Partner { Id = 99, Name = "Acme Ltd." };
        var product = new Product { Id = 1, Name = "Widget", SKU = "Test SKU", Category = null! };
        var template = new Template
        {
            Id = 42,
            PartnerId = 99,
            Partner = partner,
            Name = "Standard order",
            Type = DomainType.Sale
        };

        var item = new TemplateItem
        {
            Id = 5,
            ProductId = product.Id,
            Product = product,
            TemplateId = template.Id,
            Template = template,
            Quantity = 2,
            UnitPrice = 7.50m,
            DiscountAmount = .50m
        };

        template.Items = [item];

        // Act
        var dto = template.ToDto();

        // Assert – template-level
        Assert.Equal(42, dto.Id);
        Assert.Equal(99, dto.PartnerId);
        Assert.Equal("Acme Ltd.", dto.PartnerName);
        Assert.Equal("Standard order", dto.Name);
        Assert.Equal("Sale", dto.Type);
        Assert.Single(dto.Items);

        // Assert – item-level
        var dtoItem = dto.Items[0];
        Assert.Equal(5, dtoItem.Id);
        Assert.Equal(1, dtoItem.ProductId);
        Assert.Equal("Widget", dtoItem.ProductName);
        Assert.Equal(42, dtoItem.TemplateId);
        Assert.Equal("Standard order", dtoItem.TemplateName);
        Assert.Equal(2, dtoItem.Quantity);
        Assert.Equal(7.50m, dtoItem.UnitPrice);
        Assert.Equal(.50m, dtoItem.Discount);
    }

    [Fact]
    public void ToCreateResponse_ShouldThrowInvalidOperationException_WhenPartnerIsNull()
    {
        // Arrange
        var template = new Template
        {
            Id = 7,
            PartnerId = 11,
            Partner = null!,
            Name = "Bad template",
            Type = DomainType.Supply,
            Items = []
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToCreateResponse());
    }

    [Fact]
    public void ToCreateResponse_ShouldMapAllFieldsCorrectly_WhenTemplateHasPartnerAndItems()
    {
        // Arrange
        var partner = new Partner { Id = 11, Name = "Globex" };
        var product = new Product { Id = 2, Name = "Bolt", SKU = "Test SKU", Category = null! };
        var template = new Template
        {
            Id = 7,
            PartnerId = 11,
            Partner = partner,
            Name = "Initial order",
            Type = DomainType.Supply
        };

        var item = new TemplateItem
        {
            Id = 8,
            ProductId = product.Id,
            Product = product,
            TemplateId = template.Id,
            Template = template,
            Quantity = 12,
            UnitPrice = 1.25m,
            DiscountAmount = .10m
        };

        template.Items = [item];

        // Act
        var response = template.ToCreateResponse();

        // Assert
        Assert.Equal(7, response.Id);
        Assert.Equal(11, response.PartnerId);
        Assert.Equal("Globex", response.PartnerName);
        Assert.Equal("Initial order", response.Name);
        Assert.Equal("Supply", response.Type);
        Assert.Single(response.Items);
    }

    [Fact]
    public void ToUpdateResponse_ShouldThrowInvalidOperationException_WhenPartnerIsNull()
    {
        // Arrange
        var template = new Template
        {
            Id = 13,
            PartnerId = 20,
            Partner = null!,
            Name = "Faulty",
            Type = DomainType.Sale,
            Items = []
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToUpdateResponse());
    }

    [Fact]
    public void ToUpdateResponse_ShouldMapAllFieldsCorrectly_WhenTemplateHasPartnerAndItems()
    {
        // Arrange
        var partner = new Partner { Id = 20, Name = "Initech" };
        var product = new Product { Id = 3, Name = "Cable", SKU = "Test SKU", Category = null! };
        var template = new Template
        {
            Id = 13,
            PartnerId = 20,
            Partner = partner,
            Name = "Restock plan",
            Type = DomainType.Supply
        };

        var item = new TemplateItem
        {
            Id = 44,
            ProductId = product.Id,
            Product = product,
            TemplateId = template.Id,
            Template = template,
            Quantity = 6,
            UnitPrice = 4.00m,
            DiscountAmount = 0m
        };

        template.Items = [item];

        // Act
        var response = template.ToUpdateResponse();

        // Assert
        Assert.Equal(13, response.Id);
        Assert.Equal(20, response.PartnerId);
        Assert.Equal("Initech", response.PartnerName);
        Assert.Equal("Restock plan", response.Name);
        Assert.Equal("Supply", response.Type);
        Assert.Single(response.Items);
    }

    [Fact]
    public void ToEntity_ShouldMapAllFieldsCorrectly_WhenCreateRequestIsValid()
    {
        // Arrange
        var request = new CreateTemplateRequest(
            PartnerId: 55,
            Name: "Quarterly",
            Type: ContractType.Sale,
            Items:
            [
                new CreateTemplateItem(ProductId: 9,  Quantity: 10, UnitPrice: 2.5m, Discount: 0.25m),
                new CreateTemplateItem(ProductId: 15, Quantity:  5, UnitPrice: 5.0m, Discount: 0m)
            ]);

        // Act
        var entity = request.ToEntity();

        // Assert – template
        Assert.Equal("Quarterly", entity.Name);
        Assert.Equal(55, entity.PartnerId);
        Assert.Equal(DomainType.Sale, entity.Type);
        Assert.Null(entity.Partner);
        Assert.Equal(2, entity.Items.Count);

        // Assert – first item
        foreach (var requestItem in request.Items)
        {
            var mappedItem = entity.Items.Single(x => x.ProductId == requestItem.ProductId);

            Assert.Equal(requestItem.ProductId, mappedItem.ProductId);
            Assert.Equal(requestItem.Quantity, mappedItem.Quantity);
            Assert.Equal(requestItem.UnitPrice, mappedItem.UnitPrice);
            Assert.Equal(requestItem.Discount, mappedItem.DiscountAmount);
            Assert.Null(mappedItem.Product);
            Assert.Null(mappedItem.Template);
        }
    }

    [Fact]
    public void ApplyUpdate_ShouldModifyAllFieldsCorrectly_WhenValidRequest()
    {
        // Arrange
        var partner = new Partner { Id = 77, Name = "Test Partner" };
        var template = new Template
        {
            Id = 200,
            PartnerId = 77,
            Partner = partner,
            Name = "Old plan",
            Type = DomainType.Sale,
            Items =
            [
                new TemplateItem
                {
                    Id            = 1,
                    ProductId     = 3,
                    Quantity      = 4,
                    UnitPrice     = 3m,
                    DiscountAmount = 0.5m,
                    Product = null!,
                    Template = null!,
                }
            ]
        };

        var updateRequest = new UpdateTemplateRequest(
            Id: 200,
            PartnerId: 50,
            Name: "Updated plan",
            Type: ContractType.Supply,
            Items:
            [
                new UpdateTemplateItem(Id: 99, ProductId: 10, Quantity: 8, UnitPrice: 6m, Discount: 1m),
            ]);

        // Act
        template.ApplyUpdate(updateRequest);

        // Assert
        Assert.Equal(updateRequest.PartnerId, template.PartnerId);
        Assert.Equal(updateRequest.Name, template.Name);
        Assert.Equal(updateRequest.Type.ToString(), template.Type.ToString());
        Assert.Single(template.Items);

        var updatedItem = template.Items[0];
        Assert.Equal(10, updatedItem.ProductId);
        Assert.Equal(8, updatedItem.Quantity);
        Assert.Equal(6m, updatedItem.UnitPrice);
        Assert.Equal(1m, updatedItem.DiscountAmount);

        // Navigation properties remain null – EF will set them
        Assert.Null(updatedItem.Product);
        Assert.Null(updatedItem.Template);
    }

    [Fact]
    public void ToDto_ShouldThrowInvalidOperationException_WhenTemplateItemProductIsNull()
    {
        // Arrange
        var partner = new Partner { Id = 1, Name = "Partner A" };
        var template = new Template
        {
            Id = 10,
            PartnerId = 1,
            Partner = partner,
            Name = "Template-A",
            Type = DomainType.Sale
        };

        var item = new TemplateItem
        {
            Id = 100,
            ProductId = 5,
            Product = null!,          // ← NULL PRODUCT
            TemplateId = 10,
            Template = template,
            Quantity = 1,
            UnitPrice = 2m,
            DiscountAmount = 0m
        };

        template.Items = [item];

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToDto());
    }

    [Fact]
    public void ToDto_ShouldThrowInvalidOperationException_WhenTemplateItemTemplateIsNull()
    {
        // Arrange
        var partner = new Partner { Id = 2, Name = "Partner B" };
        var product = new Product { Id = 8, Name = "Widget", SKU = "Test SKU", Category = null! };
        var template = new Template
        {
            Id = 11,
            PartnerId = 2,
            Partner = partner,
            Name = "Template-B",
            Type = DomainType.Supply
        };

        var item = new TemplateItem
        {
            Id = 101,
            ProductId = product.Id,
            Product = product,
            TemplateId = 11,
            Template = null!,
            Quantity = 3,
            UnitPrice = 1.5m,
            DiscountAmount = 0m
        };

        template.Items = [item];

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToDto());
    }

    [Fact]
    public void ToCreateResponse_ShouldThrowInvalidOperationException_WhenTemplateItemProductIsNull()
    {
        // Arrange
        var partner = new Partner { Id = 3, Name = "Partner C" };
        var template = new Template
        {
            Id = 12,
            PartnerId = 3,
            Partner = partner,
            Name = "Template-C",
            Type = DomainType.Sale
        };

        var item = new TemplateItem
        {
            Id = 102,
            ProductId = 9,
            Product = null!,
            TemplateId = 12,
            Template = template,
            Quantity = 4,
            UnitPrice = 2m,
            DiscountAmount = .5m
        };

        template.Items = [item];

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToCreateResponse());
    }

    [Fact]
    public void ToCreateResponse_ShouldThrowInvalidOperationException_WhenTemplateItemTemplateIsNull()
    {
        // Arrange
        var partner = new Partner { Id = 4, Name = "Partner D" };
        var product = new Product { Id = 10, Name = "Bolt", SKU = "Test SKU", Category = null! };
        var template = new Template
        {
            Id = 13,
            PartnerId = 4,
            Partner = partner,
            Name = "Template-D",
            Type = DomainType.Supply
        };

        var item = new TemplateItem
        {
            Id = 103,
            ProductId = product.Id,
            Product = product,
            TemplateId = 13,
            Template = null!,
            Quantity = 2,
            UnitPrice = 5m,
            DiscountAmount = 0m
        };

        template.Items = [item];

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToCreateResponse());
    }

    [Fact]
    public void ToUpdateResponse_ShouldThrowInvalidOperationException_WhenTemplateItemProductIsNull()
    {
        // Arrange
        var partner = new Partner { Id = 5, Name = "Partner E" };
        var template = new Template
        {
            Id = 14,
            PartnerId = 5,
            Partner = partner,
            Name = "Template-E",
            Type = DomainType.Sale
        };

        var item = new TemplateItem
        {
            Id = 104,
            ProductId = 11,
            Product = null!,
            TemplateId = 14,
            Template = template,
            Quantity = 7,
            UnitPrice = 3m,
            DiscountAmount = 0m
        };

        template.Items = [item];

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToUpdateResponse());
    }

    [Fact]
    public void ToUpdateResponse_ShouldThrowInvalidOperationException_WhenTemplateItemTemplateIsNull()
    {
        // Arrange
        var partner = new Partner { Id = 6, Name = "Partner F" };
        var product = new Product { Id = 12, Name = "Cable", SKU = "Test SKU", Category = null! };
        var template = new Template
        {
            Id = 15,
            PartnerId = 6,
            Partner = partner,
            Name = "Template-F",
            Type = DomainType.Supply
        };

        var item = new TemplateItem
        {
            Id = 105,
            ProductId = product.Id,
            Product = product,
            TemplateId = 15,
            Template = null!,
            Quantity = 1,
            UnitPrice = 10m,
            DiscountAmount = 1m
        };

        template.Items = [item];

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => template.ToUpdateResponse());
    }

    [Fact]
    public void ContractTemplateTypeEnum_ShouldParseToDomain_ForAllValues()
    {
        foreach (var contractValue in Enum.GetValues(typeof(ContractType)).Cast<ContractType>())
        {
            var name = contractValue.ToString();
            var domainValue = Enum.Parse<DomainType>(name);
            Assert.Equal(name, domainValue.ToString());
        }
    }

    [Fact]
    public void DomainTemplateTypeEnum_ShouldParseToContract_ForAllValues()
    {
        foreach (var domainValue in Enum.GetValues(typeof(DomainType)).Cast<DomainType>())
        {
            var name = domainValue.ToString();
            var contractValue = Enum.Parse<ContractType>(name);
            Assert.Equal(name, contractValue.ToString());
        }
    }
}
