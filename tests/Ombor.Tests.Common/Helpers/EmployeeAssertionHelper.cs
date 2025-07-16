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
        Assert.Equal(expected.Role, actual.Role);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(CreateEmployeeRequest? expected, CreateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Role, actual.Role);
    }

    public static void AssertEquivalent(CreateEmployeeRequest? expected, Employee? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Role, actual.Role);
    }

    public static void AssertEquivalent(Employee? expected, CreateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Role, actual.Role);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(UpdateEmployeeRequest? expected, UpdateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Role, actual.Role);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(UpdateEmployeeRequest? expected, Employee? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Role, actual.Role);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    public static void AssertEquivalent(Employee? expected, UpdateEmployeeResponse? actual)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.FullName, actual.FullName);
        Assert.Equal(expected.Role, actual.Role);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }
}