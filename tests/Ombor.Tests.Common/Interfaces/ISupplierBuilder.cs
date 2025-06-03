using Ombor.Domain.Entities;

namespace Ombor.Tests.Common.Interfaces;

/// <summary>
/// Fluent builder for creating <see cref="Supplier"/> instances,
/// allowing overrides for any field and two build modes:
/// <list type="bullet">
///   <item>
///     <term><c><see cref="Build"/></c></term>
///     <description> applies explicit overrides and defaults for required fields.</description>
///   </item>
///   <item>
///     <term><c><see cref="BuildAndPopulate"/></c></term>
///     <description> applies explicit overrides and random values for all unset properties.</description>
///   </item>
/// </list>
/// </summary>
public interface ISupplierBuilder
{
    /// <summary>
    /// Specifies the <see cref="Supplier.Id"/>.
    /// If <paramref name="id"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="id">The supplier ID, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ISupplierBuilder WithId(int? id = null);

    /// <summary>
    /// Specifies the <see cref="Supplier.Name"/>.
    /// If <paramref name="name"/> is <c>null</c>, a random name will be assigned.
    /// </summary>
    /// <param name="name">The supplier name, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ISupplierBuilder WithName(string? name = null);

    /// <summary>
    /// Specifies the <see cref="Supplier.Address"/>.
    /// If <paramref name="address"/> is <c>null</c>, a random address will be assigned.
    /// </summary>
    /// <param name="address">The supplier address, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns> 
    ISupplierBuilder WithAddress(string? address = null);

    /// <summary>
    /// Specifies the <see cref="Supplier.Email"/>.
    /// If <paramref name="email"/> is <c>null</c>, a random email will be assigned.
    /// </summary>
    /// <param name="email">The supplier email, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ISupplierBuilder WithEmail(string? email = null);

    /// <summary>
    /// Specifies the <see cref="Supplier.CompanyName"/>.
    /// If <paramref name="companyName"/> is <c>null</c>, a random company name will be assigned.
    /// </summary>
    /// <param name="companyName">The supplier company name, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ISupplierBuilder WithCompanyName(string? companyName = null);

    /// <summary>
    /// Specifies the <see cref="Supplier.IsActive"/>.
    /// A random status will be assigned. To generate a random one.
    /// </summary>
    /// <returns>The same builder instance.</returns>
    ISupplierBuilder WithIsActive(bool? isActive = false);

    /// <summary>
    /// Specifies the <see cref="Supplier.PhoneNumbers"/>.
    /// If <paramref name="phoneNumbers"/> is <c>null</c>, a random phone numbers will be assigned.
    /// </summary>
    /// <param name="phoneNumbers">The supplier phone numbers, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    ISupplierBuilder WithPhoneNumbers(List<string>? phoneNumbers = null);

    /// <summary>
    /// Builds a <see cref="Supplier"/> using only explicitly set values.
    /// Unset properties default to:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>Name</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Address</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Email</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>CompanyName</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>IsActive</c></term><description> <c>false</c> if not set.</description></item>
    ///   <item><term><c>PhoneNumbers</c></term><description> <c>null</c> if not set.</description></item>
    /// </list>
    /// </summary>
    /// <returns>A new <see cref="Supplier"/> populated with only explicitly set and required fields.</returns>
    Supplier Build();

    /// <summary>
    /// Builds a <see cref="Supplier"/> using only explicitly set values.
    /// Unset properties default to:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>Name</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Address</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Email</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>CompanyName</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>IsActive</c></term><description> <c>false</c> if not set.</description></item>
    ///   <item><term><c>PhoneNumbers</c></term><description> <c>null</c> if not set.</description></item>
    /// </list>
    /// </summary>
    /// <returns>A fully populated <see cref="Supplier"/>.</returns>
    Supplier BuildAndPopulate();
}
