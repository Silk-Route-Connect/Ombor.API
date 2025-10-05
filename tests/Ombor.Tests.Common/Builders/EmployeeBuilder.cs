using Bogus;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class EmployeeBuilder(Faker faker) : BuilderBase(faker), IEmployeeBuilder
{
    private int? _id;
    private string? _fullName;
    private string? _position;
    private decimal? _salary;
    private EmployeeStatus? _status;
    private DateOnly? _dateOfEmployment;
    private ContactInfo? _contactInfo;

    public IEmployeeBuilder WithId(int? id = null)
    {
        _id = id ?? _faker.Random.Number();

        return this;
    }

    public IEmployeeBuilder WithFullName(string? fullName = null)
    {
        _fullName = fullName ?? _faker.Person.FullName;

        return this;
    }

    public IEmployeeBuilder WithSalary(decimal? salary = null)
    {
        _salary = salary ?? _faker.Random.Decimal(100, 10000);

        return this;
    }

    public IEmployeeBuilder WithPosition(string? position = null)
    {
        _position = position ?? _faker.Name.JobTitle();

        return this;
    }

    public IEmployeeBuilder WithStatus(EmployeeStatus? status = null)
    {
        _status = status ?? _faker.PickRandom<EmployeeStatus>();

        return this;
    }

    public IEmployeeBuilder WithDateOfEmployment(DateOnly? dateOfEmployment = null)
    {
        _dateOfEmployment = dateOfEmployment ?? _faker.Date.PastDateOnly();

        return this;
    }

    public IEmployeeBuilder WithContactInfo(ContactInfo? contactInfo = null)
    {
        _contactInfo = contactInfo ?? GetRandomContactInfo();

        return this;
    }

    public Employee Build() =>
      new()
      {
          Id = _id ?? default,
          FullName = _fullName ?? string.Empty,
          Position = _position ?? string.Empty,
          Salary = _salary ?? default,
          Status = _status ?? default,
          DateOfEmployment = _dateOfEmployment ?? default,
          ContactInfo = _contactInfo ?? default
      };

    public Employee BuildAndPopulate()
    {
        var id = _id ?? _faker.Random.Number();

        return new()
        {
            Id = _id ?? id,
            FullName = _fullName ?? _faker.Person.FullName,
            Salary = _salary ?? _faker.Random.Decimal(100, 10000),
            Position = _position ?? _faker.Name.JobTitle(),
            Status = _status ?? _faker.PickRandom<EmployeeStatus>(),
            DateOfEmployment = _dateOfEmployment ?? _faker.Date.PastDateOnly(),
            ContactInfo = _contactInfo ?? GetRandomContactInfo()
        };
    }

    private ContactInfo GetRandomContactInfo() =>
        new()
        {
            Email = _faker.Person.Email,
            Address = _faker.Address.StreetAddress(),
            PhoneNumbers = [_faker.Phone.PhoneNumber("+998-##-###-##-##")],
            TelegramAccount = _faker.Internet.UserName()
        };
}