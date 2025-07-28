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
    /// <summary>
    /// Asserts that a <see cref="Partner"/> entity and <see cref="PartnerDto"/> have equivalent values for all mapped properties.
    /// </summary>
    /// <param name="expected">The source <see cref="Partner"/> entity.</param>
    /// <param name="actual">The <see cref="PartnerDto"/> to verify.</param>
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
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    public static void AssertEquivalent(PartnerBalance? expected, PartnerBalanceDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Total, actual.Total);
        Assert.Equal(expected.PartnerAdvance, actual.PartnerAdvance);
        Assert.Equal(expected.CompanyAdvance, actual.CompanyAdvance);
        Assert.Equal(expected.PayableDebt, actual.PayableDebt);
        Assert.Equal(expected.ReceivableDebt, actual.ReceivableDebt);
    }

    /// <summary>
    /// Asserts that a <see cref="CreatePartnerRequest"/> and <see cref="CreatePartnerResponse"/> share idenrical request and response values.
    /// </summary>
    /// <param name="expected">The original create request.</param>
    /// <param name="actual">The response returned by the CreateAsync method.</param>
    public static void AssertEquivalent(CreatePartnerRequest? expected, CreatePartnerResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type);
        Assert.Equal(expected.Balance, actual.Balance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that a <see cref="CreatePartnerRequest"/> has been correctly mapped to a <see cref="Partner"/> entity.
    /// </summary>
    /// <param name="expected">The original create request.</param>
    /// <param name="actual">The entity created by the service.</param>
    public static void AssertEquivalent(CreatePartnerRequest? expected, Partner? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.Type.ToString(), actual.Type.ToString());
        Assert.Equal(expected.Balance, actual.Balance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that a <see cref="Partner"/> entity mathces the values returned in a <see cref="CreatePartnerResponse"/>, including the assigned Id.
    /// </summary>
    /// <param name="expected">The saved <see cref="Partner"/> entity.</param>
    /// <param name="actual">The response DTO from CreateAsync.</param>
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
        Assert.Equal(expected.Balance, actual.Balance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that an <see cref="UpdatePartnerRequest"/> and <see cref="UpdatePartnerResponse"/> share identical update values.
    /// </summary>
    /// <param name="expected">The update request.</param>
    /// <param name="actual">The response returned bt the UpdateAsync method.</param>
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
        Assert.Equal(expected.Balance, actual.Balance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that an <see cref="UpdatePartnerRequest"/> has been applied correctly to a <see cref="UpdatePartnerResponse"/> entity.
    /// </summary>
    /// <param name="expected">The update request.</param>
    /// <param name="actual">The entity after ApplyUpdate.</param>
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
        Assert.Equal(expected.Balance, actual.Balance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that a <see cref="Partner"/> entity matches the values returned in an <see cref="UpdatePartnerResponse"/> including the assigned Id.
    /// </summary>
    /// <param name="expected">The updated <see cref="Partner"/> entity.</param>
    /// <param name="actual">The response DTO from UpdateAsync.</param>
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
        Assert.Equal(expected.Balance, actual.Balance);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }
}
