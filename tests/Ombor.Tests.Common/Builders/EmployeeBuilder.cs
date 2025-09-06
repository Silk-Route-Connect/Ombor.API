using Bogus;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class EmployeeBuilder(Faker faker) : BuilderBase(faker), IEmployeeBuilder
{
    private int? _id;
    private string? _fullName;
    private decimal? _salary;
    private string? _phoneNumber;
    private string? _email;
    private string? _address;
    private string? _description;
    private EmployeePosition? _position;
    private EmployeeStatus? _status;
    private DateOnly? _dateOfEmployment;

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

    public IEmployeeBuilder WithPhoneNumber(string? phoneNumber = null)
    {
        _phoneNumber = phoneNumber ?? _faker.Phone.PhoneNumber("+998-##-###-##-##");

        return this;
    }

    public IEmployeeBuilder WithEmail(string? email = null)
    {
        _email = email ?? _faker.Person.Email;

        return this;
    }

    public IEmployeeBuilder WithAddress(string? address = null)
    {
        _address = address ?? _faker.Address.StreetAddress();

        return this;
    }

    public IEmployeeBuilder WithDescription(string? description = null)
    {
        _description = description ?? _faker.Lorem.Sentence();

        return this;
    }

    public IEmployeeBuilder WithPosition(EmployeePosition? position = null)
    {
        _position = position ?? _faker.PickRandom<EmployeePosition>();

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

    public Employee Build() =>
      new()
      {
          Id = _id ?? default,
          FullName = _fullName ?? string.Empty,
          Salary = _salary ?? default,
          PhoneNumber = _phoneNumber ?? string.Empty,
          Email = _email ?? string.Empty,
          Address = _address ?? string.Empty,
          Description = _description ?? string.Empty,
          Position = _position ?? default,
          Status = _status ?? default,
          DateOfEmployment = _dateOfEmployment ?? default,
      };

    public Employee BuildAndPopulate()
    {
        var id = _id ?? _faker.Random.Number();

        return new()
        {
            Id = _id ?? id,
            FullName = _fullName ?? _faker.Person.FullName,
            Salary = _salary ?? _faker.Random.Decimal(100, 10000),
            PhoneNumber = _phoneNumber ?? faker.Phone.PhoneNumber("+998-##-###-##-##"),
            Email = _email ?? _faker.Person.Email,
            Address = _address ?? _faker.Address.StreetAddress(),
            Description = _description ?? _faker.Lorem.Sentence(),
            Position = _position ?? _faker.PickRandom<EmployeePosition>(),
            Status = _status ?? _faker.PickRandom<EmployeeStatus>(),
            DateOfEmployment = _dateOfEmployment ?? _faker.Date.PastDateOnly(),
        };
    }
}