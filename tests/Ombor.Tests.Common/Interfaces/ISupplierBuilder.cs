using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Common.Interfaces;

/// <summary>
/// Fluent builder for creating <see cref="Partner"/> instances,
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
public interface IPartnerBuilder
{
    /// <summary>
    /// Specifies the <see cref="Partner.Id"/>.
    /// If <paramref name="id"/> is <c>null</c>, a random integer will be assigned.
    /// </summary>
    /// <param name="id">The partner ID, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IPartnerBuilder WithId(int? id = null);

    /// <summary>
    /// Specifies the <see cref="Partner.Name"/>.
    /// If <paramref name="name"/> is <c>null</c>, a random name will be assigned.
    /// </summary>
    /// <param name="name">The partner name, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IPartnerBuilder WithName(string? name = null);

    /// <summary>
    /// Specifies the <see cref="Partner.Address"/>.
    /// If <paramref name="address"/> is <c>null</c>, a random address will be assigned.
    /// </summary>
    /// <param name="address">The partner address, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IPartnerBuilder WithAddress(string? address = null);

    /// <summary>
    /// Specifies the <see cref="Partner.Email"/>.
    /// If <paramref name="email"/> is <c>null</c>, a random email will be assigned.
    /// </summary>
    /// <param name="email">The partner email, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IPartnerBuilder WithEmail(string? email = null);

    /// <summary>
    /// Specifies the <see cref="Partner.CompanyName"/>.
    /// If <paramref name="companyName"/> is <c>null</c>, a random company name will be assigned.
    /// </summary>
    /// <param name="companyName">The partner company name, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IPartnerBuilder WithCompanyName(string? companyName = null);

    /// <summary>
    /// Specifies the <see cref="Partner.PhoneNumbers"/>.
    /// If <paramref name="phoneNumbers"/> is <c>null</c>, a random phone numbers will be assigned.
    /// </summary>
    /// <param name="phoneNumbers">The partner phone numbers, or <c>null</c> to generate a random one.</param>
    /// <returns>The same builder instance.</returns>
    IPartnerBuilder WithPhoneNumbers(List<string>? phoneNumbers = null);

    /// <summary>
    /// Specifies the <see cref="Partner.Type"/>.
    /// If <paramref name="type"/> is <c>null</c>, a random type will be assigned.
    /// </summary>
    /// <param name="type">The type of the partner.</param>
    /// <returns>The same builder instance</returns>
    IPartnerBuilder WithType(PartnerType? type = null);

    /// <summary>
    /// Builds a <see cref="Partner"/> using only explicitly set values.
    /// Unset properties default to:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>Name</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Address</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Email</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>CompanyName</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>PhoneNumbers</c></term><description> <c>null</c> if not set.</description></item>
    ///   <item><term><c>Type</c></term><description> <see cref="PartnerType.All"/> if not set.</description></item>
    /// </list>
    /// </summary>
    /// <returns>A new <see cref="Partner"/> populated with only explicitly set and required fields.</returns>
    Partner Build();

    /// <summary>
    /// Builds a <see cref="Partner"/> using only explicitly set values.
    /// Unset properties default to:
    /// <list type="bullet">
    ///   <item><term><c>Id</c></term><description> <c>0</c> if not set.</description></item>
    ///   <item><term><c>Name</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Address</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>Email</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>CompanyName</c></term><description> <see cref="string.Empty"/> if not set.</description></item>
    ///   <item><term><c>PhoneNumbers</c></term><description> <c>null</c> if not set.</description></item>
    ///   <item><term><c>Type</c></term><description> <see cref="PartnerType.All"/> if not set.</description></item>
    /// </list>
    /// </summary>
    /// <returns>A fully populated <see cref="Partner"/>.</returns>
    Partner BuildAndPopulate();
}
