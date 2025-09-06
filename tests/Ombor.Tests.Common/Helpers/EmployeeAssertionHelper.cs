using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

public static class EmployeeAssertionHelper
{
    public static void AssertEquivalent(Employee? expected, EmployeeDto? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Position, Enum.Parse<EmployeePosition>(actual.Position.ToString()));
        Assert.Equal(expected.Status, Enum.Parse<EmployeeStatus>(actual.Status.ToString()));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(CreateEmployeeRequest? expected, CreateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(CreateEmployeeRequest? expected, Employee? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(Employee? expected, CreateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Position, Enum.Parse<EmployeePosition>(actual.Position.ToString()));
        Assert.Equal(expected.Status, Enum.Parse<EmployeeStatus>(actual.Status.ToString()));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(UpdateEmployeeRequest? expected, UpdateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Position, Enum.Parse<Contracts.Enums.EmployeePosition>(actual.Position.ToString()));
        Assert.Equal(expected.Status, Enum.Parse<Contracts.Enums.EmployeeStatus>(actual.Status.ToString()));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(UpdateEmployeeRequest? expected, Employee? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(Enum.Parse<EmployeePosition>(actual.Position.ToString()), actual.Position);
        Assert.Equal(Enum.Parse<EmployeeStatus>(actual.Status.ToString()), actual.Status);
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(Employee? expected, UpdateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.PhoneNumber, actual.PhoneNumber);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Position, Enum.Parse<EmployeePosition>(actual.Position.ToString()));
        Assert.Equal(expected.Status, Enum.Parse<EmployeeStatus>(actual.Status.ToString()));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }
}