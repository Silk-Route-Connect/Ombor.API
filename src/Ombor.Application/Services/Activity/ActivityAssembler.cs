using System.Text.Json;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Activity;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using DomainPaymentType = Ombor.Domain.Enums.PaymentType;
using DomainTransactionType = Ombor.Domain.Enums.TransactionType;

namespace Ombor.Application.Services.Activity;

/// <summary>
/// Builds one Activity Log item from an operation's audit rows: picks the record the operation is about, names it,
/// gives the money figure, and lists every change with readable fields.
/// </summary>
internal static class ActivityAssembler
{
    public static ActivityItemDto Build(Guid operationId, IReadOnlyCollection<ActivityRow> rows, int changeCount, ActivityLookups lookups)
    {
        var ordered = rows
            .GroupBy(row => (row.EntityType, row.EntityId))
            .Select(record => ActivityRow.Merge([.. record.OrderBy(row => row.Data.Id)]))
            .OrderBy(row => row.Rank)
            .ThenBy(row => ActionOrder(row.Action))
            .ThenBy(row => row.Data.Id)
            .ToList();
        var lead = ordered[0];
        var (entityType, entityId, action) = Subject(lead);
        var kind = KindOf(entityType, entityId, entityType == lead.EntityType ? lead : null, lookups);
        var actorId = rows.Select(row => row.Data.UserId).FirstOrDefault(id => id is not null);

        return new ActivityItemDto(
            operationId,
            rows.Min(row => row.Data.TimestampUtc),
            actorId is { } id ? new ActivityActorDto(id, lookups.Name(nameof(User), id) ?? string.Empty) : null,
            ActivityKindOf(kind, action, lead),
            new ActivityRecordDto(kind, entityId, LabelOf(entityType, entityId, entityType == lead.EntityType ? lead : null, lookups)),
            AmountOf(entityType, entityId, action, lead, lookups),
            [.. ordered.Select(row => ChangeOf(row, lookups))],
            changeCount);
    }

    private static ActivityChangeDto ChangeOf(ActivityRow row, ActivityLookups lookups) =>
        new(
            KindOf(row.EntityType, row.EntityId, row, lookups),
            row.EntityId,
            LabelOf(row.EntityType, row.EntityId, row, lookups),
            ToActivityAction(row.Action),
            ActivityFields.Build(row, lookups));

    // A part standing alone (a template whose items alone changed) speaks for the record it belongs to.
    private static (string EntityType, int EntityId, AuditAction Action) Subject(ActivityRow lead) =>
        ActivityCatalog.IsPart(lead.EntityType)
        && lead.Data is { ParentEntityType: { } parentType, ParentEntityId: { } parentId }
        && ActivityCatalog.IsKnown(parentType)
            ? (parentType, parentId, AuditAction.Updated)
            : (lead.EntityType, lead.EntityId, lead.Action);

    private static ActivityEntityKind KindOf(string entityType, int entityId, ActivityRow? row, ActivityLookups lookups)
    {
        var document = lookups.Document(entityType, entityId);

        return ActivityCatalog.KindOf(
            entityType,
            document?.TransactionType ?? SnapshotEnum<DomainTransactionType>(row, nameof(TransactionRecord.Type)),
            document?.PaymentType ?? SnapshotEnum<DomainPaymentType>(row, nameof(Payment.Type)));
    }

    private static ActivityKind ActivityKindOf(ActivityEntityKind kind, AuditAction action, ActivityRow lead) => (kind, action) switch
    {
        (ActivityEntityKind.Payroll, AuditAction.Created) => ActivityKind.PayrollPaid,
        (ActivityEntityKind.Order, AuditAction.Updated) when lead.EntityType == ActivityCatalog.Order
            && lead.New.ContainsKey(nameof(Order.Status)) => ActivityKind.OrderStatusChanged,
        (ActivityEntityKind.Stock, _) => ActivityKind.StockChanged,
        _ => Enum.TryParse<ActivityKind>($"{kind}{action}", out var composed) ? composed : ActivityKind.Other,
    };

    private static string? LabelOf(string entityType, int entityId, ActivityRow? row, ActivityLookups lookups) => entityType switch
    {
        ActivityCatalog.Transaction or ActivityCatalog.Payment => NumberOf(entityType, entityId, row, nameof(TransactionRecord.Number), lookups),
        ActivityCatalog.Order => NumberOf(entityType, entityId, row, nameof(Order.OrderNumber), lookups),
        nameof(StockAdjustment) or nameof(Transfer) or nameof(WalletTransfer) => entityId.ToString(),
        nameof(OpeningStock) or nameof(TransactionLine) or nameof(OrderLine) or nameof(TemplateItem) or nameof(TransferLine) =>
            ProductNameOf(entityType, entityId, row, lookups),
        nameof(WarehouseItem) => StockLabelOf(entityId, row, lookups),
        nameof(PaymentComponent) => ReferenceOf(entityType, entityId, row, nameof(PaymentComponent.WalletId), lookups) is { } walletId
            ? lookups.Name(nameof(Wallet), walletId)
            : null,
        nameof(PaymentAllocation) => ReferenceOf(entityType, entityId, row, nameof(PaymentAllocation.TransactionId), lookups) is { } transactionId
            ? lookups.Document(ActivityCatalog.Transaction, transactionId)?.Number?.ToString()
            : null,
        nameof(OrderStatusEvent) => null,
        _ => lookups.Name(entityType, entityId) ?? SnapshotName(row),
    };

    private static decimal? AmountOf(string entityType, int entityId, AuditAction action, ActivityRow lead, ActivityLookups lookups)
    {
        var row = entityType == lead.EntityType ? lead : null;

        return entityType switch
        {
            ActivityCatalog.Transaction => lookups.Document(entityType, entityId)?.Amount ?? row?.DecimalValue(nameof(TransactionRecord.TotalDue)),
            ActivityCatalog.Order => lookups.Document(entityType, entityId)?.Amount ?? row?.DecimalValue(nameof(Order.TotalAmount)),
            ActivityCatalog.Payment or nameof(StockAdjustment) or nameof(OpeningStock) => lookups.Document(entityType, entityId)?.Amount,
            nameof(WalletTransfer) => lookups.Document(entityType, entityId)?.Amount ?? row?.DecimalValue(nameof(WalletTransfer.Amount)),
            nameof(Partner) or nameof(Wallet) when action == AuditAction.Created
                && row?.DecimalValue(nameof(Partner.OpeningBalance)) is { } opening && opening != 0 => opening,
            _ => null,
        };
    }

    private static string? NumberOf(string entityType, int entityId, ActivityRow? row, string numberColumn, ActivityLookups lookups) =>
        (lookups.Document(entityType, entityId)?.Number ?? row?.IntValue(numberColumn))?.ToString();

    private static string? ProductNameOf(string entityType, int entityId, ActivityRow? row, ActivityLookups lookups) =>
        (row?.IntValue(nameof(TransactionLine.ProductId)) ?? lookups.Part(entityType, entityId)?.ProductId) is { } productId
            ? lookups.Name(nameof(Product), productId)
            : null;

    private static string? StockLabelOf(int entityId, ActivityRow? row, ActivityLookups lookups)
    {
        var product = ProductNameOf(nameof(WarehouseItem), entityId, row, lookups);
        var warehouseId = row?.IntValue(nameof(WarehouseItem.WarehouseId)) ?? lookups.Part(nameof(WarehouseItem), entityId)?.WarehouseId;
        var warehouse = warehouseId is { } id ? lookups.Name(nameof(Warehouse), id) : null;

        return warehouse is null ? product : $"{product} · {warehouse}";
    }

    private static int? ReferenceOf(string entityType, int entityId, ActivityRow? row, string column, ActivityLookups lookups)
    {
        var part = lookups.Part(entityType, entityId);

        return row?.IntValue(column) ?? (column == nameof(PaymentComponent.WalletId) ? part?.WalletId : part?.TransactionId);
    }

    // A deleted record is gone from its table; its last recorded name is in the audit row.
    private static string? SnapshotName(ActivityRow? row) =>
        row?.StringValue(nameof(Product.Name))
        ?? row?.StringValue(nameof(Employee.FullName))
        ?? (row?.StringValue(nameof(User.FirstName)) is { } first ? $"{first} {row.StringValue(nameof(User.LastName))}".Trim() : null);

    // Rows store enums by name; rows recorded before that stored their numbers.
    private static TEnum? SnapshotEnum<TEnum>(ActivityRow? row, string column)
        where TEnum : struct, Enum =>
        row?.Value(column) switch
        {
            { ValueKind: JsonValueKind.String } text when Enum.TryParse<TEnum>(text.GetString(), out var parsed) => parsed,
            { ValueKind: JsonValueKind.Number } number when number.TryGetInt32(out var value) => (TEnum)Enum.ToObject(typeof(TEnum), value),
            _ => null,
        };

    private static int ActionOrder(AuditAction action) => action switch
    {
        AuditAction.Created => 0,
        AuditAction.Deleted => 1,
        AuditAction.Archived => 2,
        AuditAction.Restored => 3,
        _ => 4,
    };

    public static ActivityAction ToActivityAction(AuditAction action) => action switch
    {
        AuditAction.Created => ActivityAction.Created,
        AuditAction.Deleted => ActivityAction.Deleted,
        AuditAction.Archived => ActivityAction.Archived,
        AuditAction.Restored => ActivityAction.Restored,
        _ => ActivityAction.Updated,
    };

    public static AuditAction ToAuditAction(ActivityAction action) => action switch
    {
        ActivityAction.Created => AuditAction.Created,
        ActivityAction.Deleted => AuditAction.Deleted,
        ActivityAction.Archived => AuditAction.Archived,
        ActivityAction.Restored => AuditAction.Restored,
        _ => AuditAction.Updated,
    };
}
