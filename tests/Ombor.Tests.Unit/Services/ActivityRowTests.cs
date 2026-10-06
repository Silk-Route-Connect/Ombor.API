using Ombor.Application.Services.Activity;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Unit.Services;

/// <summary>
/// How the Activity Log reads a record saved more than once in one request, and which columns the audit log keeps.
/// </summary>
public sealed class ActivityRowTests
{
    private static readonly Guid Operation = Guid.NewGuid();

    [Fact]
    public void Merge_CreatedThenSettled_IsOneCreationWithTheFinalValues()
    {
        var merged = ActivityRow.Merge(
        [
            Row(1, nameof(TransactionRecord), AuditAction.Created, null, """{"TotalDue":100,"TotalPaid":0,"Status":"Open"}"""),
            Row(2, nameof(TransactionRecord), AuditAction.Updated, """{"TotalPaid":0,"Status":"Open"}""", """{"TotalPaid":100,"Status":"Closed"}"""),
        ]);

        Assert.Equal(AuditAction.Created, merged.Action);
        Assert.Empty(merged.Old);
        Assert.Equal(100m, merged.DecimalValue("TotalDue"));
        Assert.Equal(100m, merged.DecimalValue("TotalPaid"));
        Assert.Equal("Closed", merged.StringValue("Status"));
    }

    [Fact]
    public void Merge_TwoEdits_GoFromTheFirstOldValueToTheLastNewOne_AndDropWhatEndedUnchanged()
    {
        var merged = ActivityRow.Merge(
        [
            Row(1, nameof(Product), AuditAction.Updated, """{"Name":"A","SalePrice":10}""", """{"Name":"B","SalePrice":20}"""),
            Row(2, nameof(Product), AuditAction.Updated, """{"Name":"B","SalePrice":20}""", """{"Name":"C","SalePrice":10}"""),
        ]);

        Assert.Equal(AuditAction.Updated, merged.Action);
        Assert.Equal("A", merged.Old["Name"].GetString());
        Assert.Equal("C", merged.New["Name"].GetString());
        Assert.False(merged.New.ContainsKey("SalePrice"));
    }

    [Fact]
    public void Merge_KeepsAMaskedField_ThatHasNoValues()
    {
        var merged = ActivityRow.Merge(
        [
            Row(1, nameof(User), AuditAction.Updated, """{"Password":null}""", """{"Password":null}"""),
            Row(2, nameof(User), AuditAction.Updated, """{"FirstName":"A"}""", """{"FirstName":"B"}"""),
        ]);

        Assert.True(merged.New.ContainsKey("Password"));
        Assert.True(merged.New.ContainsKey("FirstName"));
    }

    [Fact]
    public void Merge_ArchiveInTheOperation_IsTheAction()
    {
        var merged = ActivityRow.Merge(
        [
            Row(1, nameof(Product), AuditAction.Updated, """{"Name":"A"}""", """{"Name":"B"}"""),
            Row(2, nameof(Product), AuditAction.Archived, """{"IsArchived":false}""", """{"IsArchived":true}"""),
        ]);

        Assert.Equal(AuditAction.Archived, merged.Action);
    }

    [Theory]
    [InlineData(nameof(User.PasswordHash))]
    [InlineData(nameof(User.PasswordSalt))]
    public void FieldPolicy_RecordsAPasswordChange_ByNameOnly(string property)
    {
        Assert.Equal("Password", AuditFieldPolicy.MaskedName(typeof(User), property));
        Assert.True(AuditFieldPolicy.IsMaskedField(typeof(User), "Password"));
    }

    [Theory]
    [InlineData(typeof(User), nameof(User.Language))]
    [InlineData(typeof(Product), nameof(Product.Id))]
    [InlineData(typeof(Product), nameof(Product.OrganizationId))]
    [InlineData(typeof(Warehouse), nameof(Warehouse.UpdatedAt))]
    [InlineData(typeof(TransactionRecord), nameof(TransactionRecord.CreatedById))]
    public void FieldPolicy_LeavesOutBookkeepingAndPreferences(Type entityType, string property) =>
        Assert.True(AuditFieldPolicy.IsExcluded(entityType, property));

    [Fact]
    public void Catalog_KnowsEveryAuditedEntity()
    {
        var audited = typeof(EntityBase).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IAuditable).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToList();

        Assert.NotEmpty(audited);
        Assert.All(audited, name => Assert.True(ActivityCatalog.IsKnown(name), $"{name} has no Activity Log kind."));
    }

    [Fact]
    public void FieldPolicy_KeepsBusinessColumns() =>
        Assert.False(AuditFieldPolicy.IsExcluded(typeof(Product), nameof(Product.SalePrice)));

    private static ActivityRow Row(int id, string entityType, AuditAction action, string? old, string? @new) =>
        new(new ActivityRowData(id, Operation, entityType, 7, action, null, null, old, @new, 1, DateTimeOffset.UtcNow));
}
