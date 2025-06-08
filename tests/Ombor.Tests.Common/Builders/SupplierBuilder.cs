using Bogus;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class SupplierBuilder(Faker faker) : BuilderBase(faker), ISupplierBuilder
{
    private int? _id;
    private string? _name;
    private string? _address;
    private string? _email;
    private string? _companyName;
    private bool? _isActive;
    private List<string>? _phoneNumbers;

    public ISupplierBuilder WithId(int? id = null)
    {
        _id = id ?? _faker.Random.Number();

        return this;
    }

    public ISupplierBuilder WithName(string? name = null)
    {
        _name = name ?? _faker.Person.FullName;

        return this;
    }

    public ISupplierBuilder WithAddress(string? address = null)
    {
        _address = address ?? _faker.Address.FullAddress();

        return this;
    }

    public ISupplierBuilder WithEmail(string? email = null)
    {
        _email = email ?? _faker.Person.Email;

        return this;
    }

    public ISupplierBuilder WithCompanyName(string? companyName = null)
    {
        _companyName = companyName ?? _faker.Person.Company.Name;

        return this;
    }

    public ISupplierBuilder WithIsActive(bool? isActive = false)
    {
        _isActive = isActive ?? _faker.Random.Bool();

        return this;
    }

    public ISupplierBuilder WithPhoneNumbers(List<string>? phoneNumbers = null)
    {
        _phoneNumbers = phoneNumbers ?? GeneratePhoneNumbers();

        return this;
    }

    public Supplier Build()
    {
        return new()
        {
            Id = _id ?? default,
            Name = _name ?? string.Empty,
            Address = _address,
            Email = _email,
            CompanyName = _companyName,
            IsActive = _isActive ?? false,
            PhoneNumbers = _phoneNumbers ?? []
        };
    }

    public Supplier BuildAndPopulate()
    {
        return new()
        {
            Id = _id ?? _faker.Random.Number(),
            Name = _name ?? _faker.Person.FullName,
            Address = _address ?? _faker.Address.FullAddress(),
            Email = _email ?? _faker.Person.Email,
            CompanyName = _companyName ?? _faker.Company.CompanyName(),
            IsActive = _isActive ?? _faker.Random.Bool(),
            PhoneNumbers = _phoneNumbers ?? GeneratePhoneNumbers()
        };
    }

    private List<string> GeneratePhoneNumbers()
    {
        List<string> phoneNumbers = [];
        var countOfNumbers = _faker.Random.Int(1, 5);

        for (int i = 0; i < countOfNumbers; i++)
        {
            phoneNumbers.Add(_faker.Phone.PhoneNumber("+998-##-###-##-##"));
        }

        return phoneNumbers;
    }
}
