using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Supplier;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Mappings;

public class SupplierMappingsTests
{
    [Fact]
    public void ToEntity_ShouldMapAllFieldsCorrectly_WhenValidRequest()
    {
        // Arrange 
        var request = new CreateSupplierRequest(
            Name: "John",
            Address: "New York",
            Email: "qwerty@gmail.com",
            CompanyName: "qwertgvc",
            IsActive: true,
            PhoneNumbers: ["+998945558888"]
        );

        // Act 
        var entity = request.ToEntity();

        // Assert

        Assert.Equal(request.Name, entity.Name);
        Assert.Equal(request.Address, entity.Address);
        Assert.Equal(request.Email, entity.Email);
        Assert.Equal(request.CompanyName, entity.CompanyName);
        Assert.Equal(request.IsActive, entity.IsActive);
        Assert.Equal(request.PhoneNumbers, entity.PhoneNumbers);
    }

    [Fact]
    public void ToCreateResponse_ShouldMapAllFields()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = 11,
            Name = "Test Supplier",
            Address = "asdfghjk",
            Email = "testemail@gmail.com",
            CompanyName = "qwerty",
            IsActive = true,
            PhoneNumbers = ["+998944447788"]
        };

        // Act
        var response = supplier.ToCreateResponse();

        // Assert
        Assert.Equal(supplier.Id, response.Id);
        Assert.Equal(supplier.Name, response.Name);
        Assert.Equal(supplier.Address, response.Address);
        Assert.Equal(supplier.Email, response.Email);
        Assert.Equal(supplier.CompanyName, response.CompanyName);
        Assert.Equal(supplier.IsActive, response.IsActive);
        Assert.Equal(supplier.PhoneNumbers, response.PhoneNumbers);
    }

    [Fact]
    public void ToUpdateResponse_ShouldMapAllFields()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = 12,
            Name = "Test Supplier",
            Address = "asdfghjk",
            Email = "testemail@gmail.com",
            CompanyName = "qwerty",
            IsActive = true,
            PhoneNumbers = ["+998944447788"]
        };

        // Act
        var response = supplier.ToUpdateResponse();

        // Assert
        Assert.Equal(supplier.Id, response.Id);
        Assert.Equal(supplier.Name, response.Name);
        Assert.Equal(supplier.Address, response.Address);
        Assert.Equal(supplier.Email, response.Email);
        Assert.Equal(supplier.CompanyName, response.CompanyName);
        Assert.Equal(supplier.IsActive, response.IsActive);
        Assert.Equal(supplier.PhoneNumbers, response.PhoneNumbers);
    }

    [Fact]
    public void ToDto_ShouldMapAllFields()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = 13,
            Name = "Test Supplier",
            Address = "asdfghjk",
            Email = "testemail@gmail.com",
            CompanyName = "qwerty",
            IsActive = true,
            PhoneNumbers = ["+998944447788"]
        };

        // Act
        var response = supplier.ToDto();

        // Assert
        Assert.Equal(supplier.Id, response.Id);
        Assert.Equal(supplier.Name, response.Name);
        Assert.Equal(supplier.Address, response.Address);
        Assert.Equal(supplier.Email, response.Email);
        Assert.Equal(supplier.CompanyName, response.CompanyName);
        Assert.Equal(supplier.IsActive, response.IsActive);
        Assert.Equal(supplier.PhoneNumbers, response.PhoneNumbers);
    }

    [Fact]
    public void ApplyUpdate_ShouldOverwriteAllFields()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = 14,
            Name = "Test Supplier",
            Address = "asdfghjk",
            Email = "testemail@gmail.com",
            CompanyName = "qwerty",
            IsActive = true,
            PhoneNumbers = ["+998944447788"]
        };

        var request = new UpdateSupplierRequest(
            Id: 14,
            Name: "Updated Supplier Name",
            Address: "Updated address",
            Email: "Updated email",
            CompanyName: "Updated company name",
            IsActive: false,
            PhoneNumbers: ["+998885552200"]);

        // Act
        supplier.ApplyUpdate(request);

        // Assert
        Assert.Equal(request.Id, supplier.Id);
        Assert.Equal(request.Name, supplier.Name);
        Assert.Equal(request.Address, supplier.Address);
        Assert.Equal(request.Email, supplier.Email);
        Assert.Equal(request.CompanyName, supplier.CompanyName);
        Assert.Equal(request.IsActive, supplier.IsActive);
        Assert.Equal(request.PhoneNumbers, supplier.PhoneNumbers);
    }
}
