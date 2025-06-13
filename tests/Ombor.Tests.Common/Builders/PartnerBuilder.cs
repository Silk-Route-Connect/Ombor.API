using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class PartnerBuilder(Faker faker) : BuilderBase(faker), IPartnerBuilder
{
    private int? _id;
    private string? _name;
    private string? _address;
    private string? _email;
    private string? _companyName;
    private List<string>? _phoneNumbers;
    private PartnerType? _type;

    public IPartnerBuilder WithId(int? id = null)
    {
        _id = id ?? _faker.Random.Number();

        return this;
    }

    public IPartnerBuilder WithName(string? name = null)
    {
        _name = name ?? _faker.Person.FullName;

        return this;
    }

    public IPartnerBuilder WithAddress(string? address = null)
    {
        _address = address ?? _faker.Address.FullAddress();

        return this;
    }

    public IPartnerBuilder WithEmail(string? email = null)
    {
        _email = email ?? _faker.Person.Email;

        return this;
    }

    public IPartnerBuilder WithCompanyName(string? companyName = null)
    {
        _companyName = companyName ?? _faker.Person.Company.Name;

        return this;
    }

    public IPartnerBuilder WithType(PartnerType? type = null)
    {
        _type = type ?? PartnerType.All;

        return this;
    }

    public IPartnerBuilder WithPhoneNumbers(List<string>? phoneNumbers = null)
    {
        _phoneNumbers = phoneNumbers ?? GeneratePhoneNumbers();

        return this;
    }

    public Partner Build() => new()
    {
        Id = _id ?? default,
        Name = _name ?? string.Empty,
        Address = _address,
        Email = _email,
        CompanyName = _companyName,
        Type = _type ?? PartnerType.All,
        PhoneNumbers = _phoneNumbers ?? []
    };

    public Partner BuildAndPopulate() => new()
    {
        Id = _id ?? _faker.Random.Number(),
        Name = _name ?? _faker.Person.FullName,
        Address = _address ?? _faker.Address.FullAddress(),
        Email = _email ?? _faker.Person.Email,
        CompanyName = _companyName ?? _faker.Company.CompanyName(),
        Type = _type ?? _faker.Random.Enum<PartnerType>(),
        PhoneNumbers = _phoneNumbers ?? GeneratePhoneNumbers()
    };

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
