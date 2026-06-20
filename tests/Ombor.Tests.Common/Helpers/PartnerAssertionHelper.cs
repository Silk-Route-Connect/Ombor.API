using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

/// <summary>
/// Provides assertion helper methods for verifying equivalence between domain entities, request DTOs and response DTOs in xUnit tests for partners.
/// </summary>
public static class PartnerAssertionHelper
{
    public static void AssertEquivalent(Partner? expected, PartnerDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        Assert.Equal(expected.OpeningBalance, actual.OpeningBalance);
        Assert.Equal(expected.OpeningDate, actual.OpeningDate);
        Assert.Equal(expected.IsArchived, actual.IsArchived);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    public static void AssertEquivalent(CreatePartnerRequest? expected, CreatePartnerResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        Assert.Equal(expected.OpeningBalance, actual.OpeningBalance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    public static void AssertEquivalent(CreatePartnerRequest? expected, Partner? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type.ToString());
        Assert.Equal(expected.OpeningBalance, actual.OpeningBalance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    public static void AssertEquivalent(Partner? expected, CreatePartnerResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        Assert.Equal(expected.OpeningBalance, actual.OpeningBalance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    public static void AssertEquivalent(UpdatePartnerRequest? expected, UpdatePartnerResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    public static void AssertEquivalent(UpdatePartnerRequest? expected, Partner? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type.ToString());
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    public static void AssertEquivalent(Partner? expected, UpdatePartnerResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        Assert.Equal(expected.OpeningBalance, actual.OpeningBalance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }
}
