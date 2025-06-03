using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Product;
using Ombor.Domain.Entities;
using ContractMeasurement = Ombor.Contracts.Enums.UnitOfMeasurement;
using ContractType = Ombor.Contracts.Enums.ProductType;
using DomainMeasurement = Ombor.Domain.Enums.UnitOfMeasurement;
using DomainType = Ombor.Domain.Enums.ProductType;

namespace Ombor.Tests.Unit.Mappings;

public class ProductMappingsTests
{
    [Fact]
    public void ToDto_ShouldThrowInvalidOperationException_WhenCategoryIsNull()
    {
        // Arrange
        var product = new Product
        {
            Id = 42,
            Name = "Test",
            SKU = "SKU-123",
            CategoryId = 99,
            Category = null!
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.ToDto());
    }

    [Fact]
    public void ToDto_ShouldMapAllFieldsCorrectly_WhenProductHasCategory()
    {
        // Arrange
        var category = new Category { Id = 99, Name = "Widgets" };
        var product = new Product
        {
            Id = 42,
            CategoryId = 99,
            Category = category,
            Name = "Gizmo",
            SKU = "GZ-01",
            Description = "A useful gizmo",
            Barcode = "1234567890",
            SalePrice = 9.99m,
            SupplyPrice = 5.00m,
            RetailPrice = 12.50m,
            QuantityInStock = 3,
            LowStockThreshold = 5,
            Measurement = DomainMeasurement.Kilogram,
            Type = DomainType.Supply
        };

        // Act
        var dto = product.ToDto();

        // Assert
        Assert.Equal(42, dto.Id);
        Assert.Equal(99, dto.CategoryId);
        Assert.Equal("Widgets", dto.CategoryName);
        Assert.Equal("Gizmo", dto.Name);
        Assert.Equal("GZ-01", dto.SKU);
        Assert.Equal("A useful gizmo", dto.Description);
        Assert.Equal("1234567890", dto.Barcode);
        Assert.Equal(9.99m, dto.SalePrice);
        Assert.Equal(5.00m, dto.SupplyPrice);
        Assert.Equal(12.50m, dto.RetailPrice);
        Assert.Equal(3, dto.QuantityInStock);
        Assert.Equal(5, dto.LowStockThreshold);
        Assert.True(dto.IsLowStock);
        Assert.Equal("Kilogram", dto.Measurement);
        Assert.Equal("Supply", dto.Type);
    }

    [Fact]
    public void ToCreateResponse_ShouldThrowInvalidOperationException_WhenCategoryIsNull()
    {
        // Arrange
        var product = new Product
        {
            Id = 7,
            Name = "Test Product",
            SKU = "TP-01",
            CategoryId = 1,
            Category = null!
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.ToCreateResponse());
    }

    [Fact]
    public void ToCreateResponse_ShouldMapAllFieldsCorrectly_WhenProductHasCategory()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Books" };
        var product = new Product
        {
            Id = 7,
            CategoryId = 1,
            Category = category,
            Name = "C# In Depth",
            SKU = "CSHARP-01",
            Description = "Deep dive into C#",
            Barcode = "0987654321",
            SalePrice = 30m,
            SupplyPrice = 20m,
            RetailPrice = 35m,
            QuantityInStock = 10,
            LowStockThreshold = 2,
            Measurement = DomainMeasurement.Box,
            Type = DomainType.All
        };

        // Act
        var response = product.ToCreateResponse();

        // Assert
        Assert.Equal(7, response.Id);
        Assert.Equal(1, response.CategoryId);
        Assert.Equal("Books", response.CategoryName);
        Assert.Equal("C# In Depth", response.Name);
        Assert.Equal("CSHARP-01", response.SKU);
        Assert.Equal("Deep dive into C#", response.Description);
        Assert.Equal("0987654321", response.Barcode);
        Assert.Equal(30m, response.SalePrice);
        Assert.Equal(20m, response.SupplyPrice);
        Assert.Equal(35m, response.RetailPrice);
        Assert.Equal(10, response.QuantityInStock);
        Assert.Equal(2, response.LowStockThreshold);
        Assert.False(response.IsLowStock);
        Assert.Equal("Box", response.Measurement);
        Assert.Equal(nameof(DomainType.All), response.Type);
    }

    [Fact]
    public void ToUpdateResponse_ShouldThrowInvalidOperationException_WhenCategoryIsNull()
    {
        // Arrange
        var product = new Product
        {
            Id = 13,
            Name = "Test Product",
            SKU = "TP-02",
            CategoryId = 5,
            Category = null!
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => product.ToUpdateResponse());
    }

    [Fact]
    public void ToUpdateResponse_ShouldMapAllFieldsCorrectly_WhenProductHasCategory()
    {
        // Arrange
        var category = new Category { Id = 5, Name = "Garden" };
        var product = new Product
        {
            Id = 13,
            CategoryId = 5,
            Category = category,
            Name = "Shovel",
            SKU = "SHV-100",
            Description = "Sturdy shovel",
            Barcode = "555444333",
            SalePrice = 25m,
            SupplyPrice = 15m,
            RetailPrice = 28m,
            QuantityInStock = 4,
            LowStockThreshold = 2,
            Measurement = DomainMeasurement.Ton,
            Type = DomainType.Sale
        };

        // Act
        var response = product.ToUpdateResponse();

        // Assert
        Assert.Equal(13, response.Id);
        Assert.Equal(5, response.CategoryId);
        Assert.Equal("Garden", response.CategoryName);
        Assert.Equal("Shovel", response.Name);
        Assert.Equal("SHV-100", response.SKU);
        Assert.Equal("Sturdy shovel", response.Description);
        Assert.Equal("555444333", response.Barcode);
        Assert.Equal(25m, response.SalePrice);
        Assert.Equal(15m, response.SupplyPrice);
        Assert.Equal(28m, response.RetailPrice);
        Assert.Equal(4, response.QuantityInStock);
        Assert.Equal(2, response.LowStockThreshold);
        Assert.False(response.IsLowStock);
        Assert.Equal("Ton", response.Measurement);
        Assert.Equal("Sale", response.Type);
    }

    [Fact]
    public void ToEntity_ShouldMapAllFieldsCorrectly_WhenValidRequest()
    {
        // Arrange
        var request = new CreateProductRequest(
            CategoryId: 77,
            Name: "Widget",
            SKU: "WGT-01",
            Description: "Test widget",
            Barcode: "111222333",
            SalePrice: 10m,
            SupplyPrice: 6m,
            RetailPrice: 12m,
            QuantityInStock: 8,
            LowStockThreshold: 3,
            Measurement: ContractMeasurement.Piece,
            Type: ContractType.Sale,
            Attachments: []);

        // Act
        var entity = request.ToEntity();

        // Assert
        Assert.Equal("Widget", entity.Name);
        Assert.Equal("WGT-01", entity.SKU);
        Assert.Equal("Test widget", entity.Description);
        Assert.Equal("111222333", entity.Barcode);
        Assert.Equal(10m, entity.SalePrice);
        Assert.Equal(6m, entity.SupplyPrice);
        Assert.Equal(12m, entity.RetailPrice);
        Assert.Equal(8, entity.QuantityInStock);
        Assert.Equal(3, entity.LowStockThreshold);
        Assert.Equal(DomainMeasurement.Piece, entity.Measurement);
        Assert.Equal(DomainType.Sale, entity.Type);
        Assert.Equal(77, entity.CategoryId);
    }

    [Fact]
    public void ApplyUpdate_ShouldModifyAllFieldsCorrectly_WhenValidRequest()
    {
        // Arrange
        var originalCategory = new Category { Id = 9, Name = "Tools" };
        var product = new Product
        {
            Id = 21,
            CategoryId = 9,
            Category = originalCategory,
            Name = "Hammer",
            SKU = "HMR-01",
            Description = "Steel hammer",
            Barcode = "999888777",
            SalePrice = 12m,
            SupplyPrice = 7m,
            RetailPrice = 14m,
            QuantityInStock = 5,
            LowStockThreshold = 2,
            Measurement = DomainMeasurement.None,
            Type = DomainType.Supply
        };

        var updateRequest = new UpdateProductRequest(
            Id: product.Id,
            CategoryId: 10,
            Name: "Sledgehammer",
            SKU: "SDG-01",
            Description: "Heavy steel hammer",
            Barcode: "777888999",
            SalePrice: 20m,
            SupplyPrice: 10m,
            RetailPrice: 22m,
            QuantityInStock: 3,
            LowStockThreshold: 1,
            Measurement: ContractMeasurement.Ton,
            Type: ContractType.All,
            Attachments: [],
            ImagesToDelete: []);

        // Act
        product.ApplyUpdate(updateRequest);

        // Assert
        Assert.Equal("Sledgehammer", product.Name);
        Assert.Equal("SDG-01", product.SKU);
        Assert.Equal("Heavy steel hammer", product.Description);
        Assert.Equal("777888999", product.Barcode);
        Assert.Equal(20m, product.SalePrice);
        Assert.Equal(10m, product.SupplyPrice);
        Assert.Equal(22m, product.RetailPrice);
        Assert.Equal(3, product.QuantityInStock);
        Assert.Equal(1, product.LowStockThreshold);
        Assert.Equal(DomainMeasurement.Ton, product.Measurement);
        Assert.Equal(DomainType.All, product.Type);
        Assert.Equal(10, product.CategoryId);

        // Note: ApplyUpdate changes CategoryId but leaves the navigation property untouched.
        // If you expect the Category reference to update (or clear), you may need to adjust the mapping.
        Assert.Equal(9, originalCategory.Id);
        Assert.Same(originalCategory, product.Category);
    }

    [Fact]
    public void ContractUnitOfMeasurementEnum_ShouldParseToDomain_ForAllValues()
    {
        // Arrange & Act & Assert
        foreach (var contractValue in Enum.GetValues(typeof(ContractMeasurement)).Cast<ContractMeasurement>())
        {
            var name = contractValue.ToString();
            var domainValue = Enum.Parse<DomainMeasurement>(name);
            Assert.Equal(name, domainValue.ToString());
        }
    }

    [Fact]
    public void DomainUnitOfMeasurementEnum_ShouldParseToContract_ForAllValues()
    {
        // Arrange & Act & Assert
        foreach (var domainValue in Enum.GetValues(typeof(DomainMeasurement)).Cast<DomainMeasurement>())
        {
            var name = domainValue.ToString();
            var contractValue = Enum.Parse<ContractMeasurement>(name);
            Assert.Equal(name, contractValue.ToString());
        }
    }

    [Fact]
    public void ContractProductTypeEnum_ShouldParseToDomain_ForAllValues()
    {
        // Arrange & Act & Assert
        foreach (var contractValue in Enum.GetValues(typeof(ContractType)).Cast<ContractType>())
        {
            var name = contractValue.ToString();
            var domainValue = Enum.Parse<DomainType>(name);
            Assert.Equal(name, domainValue.ToString());
        }
    }

    [Fact]
    public void DomainProductTypeEnum_ShouldParseToContract_ForAllValues()
    {
        // Arrange & Act & Assert
        foreach (var domainValue in Enum.GetValues(typeof(DomainType)).Cast<DomainType>())
        {
            var name = domainValue.ToString();
            var contractValue = Enum.Parse<ContractType>(name);
            Assert.Equal(name, contractValue.ToString());
        }
    }
}
