using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Partner;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Mappings;

public class PartnerMappingsTests
{
    [Fact]
    public void ToEntity_ShouldMapAllFieldsCorrectly_WhenValidRequest()
    {
        // Arrange 
        var request = new CreatePartnerRequest(
            Name: "John",
            Address: "New York",
            Email: "qwerty@gmail.com",
            CompanyName: "qwertgvc",
            IsActive: true,
            Balance: 1000.00m,
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
        Assert.Equal(request.Balance, entity.Balance);
        Assert.Equal(request.PhoneNumbers, entity.PhoneNumbers);
    }

    [Fact]
    public void ToCreateResponse_ShouldMapAllFields()
    {
        // Arrange
        var partner = new Partner
        {
            Id = 11,
            Name = "Test partner",
            Address = "asdfghjk",
            Email = "testemail@gmail.com",
            CompanyName = "qwerty",
            IsActive = true,
            PhoneNumbers = ["+998944447788"]
        };

        // Act
        var response = partner.ToCreateResponse();

        // Assert
        Assert.Equal(partner.Id, response.Id);
        Assert.Equal(partner.Name, response.Name);
        Assert.Equal(partner.Address, response.Address);
        Assert.Equal(partner.Email, response.Email);
        Assert.Equal(partner.CompanyName, response.CompanyName);
        Assert.Equal(partner.IsActive, response.IsActive);
        Assert.Equal(partner.Balance, response.Balance);
        Assert.Equal(partner.PhoneNumbers, response.PhoneNumbers);
    }

    [Fact]
    public void ToUpdateResponse_ShouldMapAllFields()
    {
        // Arrange
        var partner = new Partner
        {
            Id = 12,
            Name = "Test partner",
            Address = "asdfghjk",
            Email = "testemail@gmail.com",
            CompanyName = "qwerty",
            IsActive = true,
            PhoneNumbers = ["+998944447788"]
        };

        // Act
        var response = partner.ToUpdateResponse();

        // Assert
        Assert.Equal(partner.Id, response.Id);
        Assert.Equal(partner.Name, response.Name);
        Assert.Equal(partner.Address, response.Address);
        Assert.Equal(partner.Email, response.Email);
        Assert.Equal(partner.CompanyName, response.CompanyName);
        Assert.Equal(partner.IsActive, response.IsActive);
        Assert.Equal(partner.Balance, response.Balance);
        Assert.Equal(partner.PhoneNumbers, response.PhoneNumbers);
    }

    [Fact]
    public void ToDto_ShouldMapAllFields()
    {
        // Arrange
        var partner = new Partner
        {
            Id = 13,
            Name = "Test partner",
            Address = "asdfghjk",
            Email = "testemail@gmail.com",
            CompanyName = "qwerty",
            IsActive = true,
            PhoneNumbers = ["+998944447788"]
        };

        // Act
        var response = partner.ToDto();

        // Assert
        Assert.Equal(partner.Id, response.Id);
        Assert.Equal(partner.Name, response.Name);
        Assert.Equal(partner.Address, response.Address);
        Assert.Equal(partner.Email, response.Email);
        Assert.Equal(partner.CompanyName, response.CompanyName);
        Assert.Equal(partner.IsActive, response.IsActive);
        Assert.Equal(partner.Balance, response.Balance);
        Assert.Equal(partner.PhoneNumbers, response.PhoneNumbers);
    }

    [Fact]
    public void ApplyUpdate_ShouldOverwriteAllFields()
    {
        // Arrange
        var partner = new Partner
        {
            Id = 14,
            Name = "Test partner",
            Address = "asdfghjk",
            Email = "testemail@gmail.com",
            CompanyName = "qwerty",
            IsActive = true,
            PhoneNumbers = ["+998944447788"]
        };

        var request = new UpdatePartnerRequest(
            Id: 14,
            Name: "Updated partner Name",
            Address: "Updated address",
            Email: "Updated email",
            CompanyName: "Updated company name",
            IsActive: false,
            Balance: 0.00m,
            PhoneNumbers: ["+998885552200"]);

        // Act
        partner.ApplyUpdate(request);

        // Assert
        Assert.Equal(request.Id, partner.Id);
        Assert.Equal(request.Name, partner.Name);
        Assert.Equal(request.Address, partner.Address);
        Assert.Equal(request.Email, partner.Email);
        Assert.Equal(request.CompanyName, partner.CompanyName);
        Assert.Equal(request.IsActive, partner.IsActive);
        Assert.Equal(request.Balance, partner.Balance);
        Assert.Equal(request.PhoneNumbers, partner.PhoneNumbers);
    }
}
