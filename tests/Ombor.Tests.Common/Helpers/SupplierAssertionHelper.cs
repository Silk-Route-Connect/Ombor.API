
using Ombor.Contracts.Requests.Supplier;
using Ombor.Contracts.Responses.Supplier;
using Ombor.Domain.Entities;
using Xunit;

namespace Ombor.Tests.Common.Helpers;

/// <summary>
/// Provides assertion helper methods for verifying equivalence between domain entities, request DTOs and response DTOs in xUnit tests for suppliers.
/// </summary>
public static class SupplierAssertionHelper
{
    /// <summary>
    /// Asserts that a <see cref="Supplier"/> entity and <see cref="SupplierDto"/> have equivalent values for all mapped properties.
    /// </summary>
    /// <param name="expected">The source <see cref="Supplier"/> entity.</param>
    /// <param name="actual">The <see cref="SupplierDto"/> to verify.</param>
    public static void AssertEquivalent(Supplier? expected, SupplierDto? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that a <see cref="CreateSupplierRequest"/> and <see cref="CreateSupplierResponse"/> share idenrical request and response values.
    /// </summary>
    /// <param name="expected">The original create request.</param>
    /// <param name="actual">The response returned by the CreateAsync method.</param>
    public static void AssertEquivalent(CreateSupplierRequest? expected, CreateSupplierResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that a <see cref="CreateSupplierRequest"/> has been correctly mapped to a <see cref="Supplier"/> entity.
    /// </summary>
    /// <param name="expected">The original create request.</param>
    /// <param name="actual">The entity created by the service.</param>
    public static void AssertEquivalent(CreateSupplierRequest? expected, Supplier? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that a <see cref="Supplier"/> entity mathces the values returned in a <see cref="CreateSupplierResponse"/>, including the assigned Id.
    /// </summary>
    /// <param name="expected">The saved <see cref="Supplier"/> entity.</param>
    /// <param name="actual">The response DTO from CreateAsync.</param>
    public static void AssertEquivalent(Supplier? expected, CreateSupplierResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that an <see cref="UpdateSupplierRequest"/> and <see cref="UpdateSupplierResponse"/> share identical update values.
    /// </summary>
    /// <param name="expected">The update request.</param>
    /// <param name="actual">The response returned bt the UpdateAsync method.</param>
    public static void AssertEquivalent(UpdateSupplierRequest? expected, UpdateSupplierResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that an <see cref="UpdateSupplierRequest"/> has been applied correctly to a <see cref="UpdateSupplierResponse"/> entity.
    /// </summary>
    /// <param name="expected">The update request.</param>
    /// <param name="actual">The entity after ApplyUpdate.</param>
    public static void AssertEquivalent(UpdateSupplierRequest? expected, Supplier? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }

    /// <summary>
    /// Asserts that a <see cref="Supplier"/> entity matches the values returned in an <see cref="UpdateSupplierResponse"/> including the assigned Id.
    /// </summary>
    /// <param name="expected">The updated <see cref="Supplier"/> entity.</param>
    /// <param name="actual">The response DTO from UpdateAsync.</param>
    public static void AssertEquivalent(Supplier? expected, UpdateSupplierResponse? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Email, actual.Email);
        Assert.Equal(expected.CompanyName, actual.CompanyName);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.PhoneNumbers, actual.PhoneNumbers);
    }
}
