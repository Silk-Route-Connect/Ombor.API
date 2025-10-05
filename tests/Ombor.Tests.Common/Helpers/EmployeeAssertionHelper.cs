using Ombor.Contracts.Requests.Employee;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
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
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.Status, Enum.Parse<Domain.Enums.EmployeeStatus>(actual.Status));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
        AssertContactInfo(expected.ContactInfo, actual.ContactInfo);
    }

    public static void AssertEquivalent(CreateEmployeeRequest? expected, CreateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.Status, Enum.Parse<Contracts.Enums.EmployeeStatus>(actual.Status));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(CreateEmployeeRequest? expected, Employee? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.Status, Enum.Parse<Contracts.Enums.EmployeeStatus>(actual.Status.ToString()));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(Employee? expected, CreateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.Status, Enum.Parse<Domain.Enums.EmployeeStatus>(actual.Status));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(UpdateEmployeeRequest? expected, UpdateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Status, Enum.Parse<Contracts.Enums.EmployeeStatus>(actual.Status));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(UpdateEmployeeRequest? expected, Employee? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(actual.Position, actual.Position);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.Status, Enum.Parse<Contracts.Enums.EmployeeStatus>(actual.Status.ToString()));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    public static void AssertEquivalent(Employee? expected, UpdateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Salary, actual.Salary);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Status, Enum.Parse<Domain.Enums.EmployeeStatus>(actual.Status));
        Assert.Equal(expected.DateOfEmployment, actual.DateOfEmployment);
    }

    private static void AssertContactInfo(Domain.Common.ContactInfo? expected, Contracts.Common.ContactInfo? actual)
    {
        if (expected is null && actual is null)
        {
            return;
        }

        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.TelegramAccount, actual.TelegramAccount);
        Assert.Equal(expected.PhoneNumbers.Length, actual.PhoneNumbers.Length);

        for (int i = 0; i < expected.PhoneNumbers.Length; i++)
        {
            Assert.Contains(expected.PhoneNumbers[i], actual.PhoneNumbers);
        }
    }
}