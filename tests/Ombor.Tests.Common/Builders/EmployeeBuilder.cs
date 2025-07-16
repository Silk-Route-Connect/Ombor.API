using Bogus;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Common.Builders;

internal sealed class EmployeeBuilder(Faker faker) : BuilderBase(faker), IEmployeeBuilder
{
    private int? _id;
    private string? _fullName;
    private string? _role;
    private bool? _isActive;

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


    public IEmployeeBuilder WithIsActive(bool? isActive = null)
    {
        _isActive = isActive ?? _faker.Random.Bool();

        return this;
    }

    public IEmployeeBuilder WithRole(string? role = null)
    {
        _role = role ?? _faker.Name.JobTitle();

        return this;
    }
    public Employee Build() =>
        new()
        {
            Id = _id ?? default,
            FullName = _fullName ?? string.Empty,
            Role = _role ?? string.Empty,
            IsActive = _isActive ?? default
        };

    public Employee BuildAndPopulate()
    {
        var id = _id ?? _faker.Random.Number();

        return new()
        {
            Id = _id ?? id,
            FullName = _fullName ?? _faker.Person.FullName,
            Role = _role ?? _faker.Name.JobTitle(),
            IsActive = _isActive ?? _faker.Random.Bool()
        };
    }
}