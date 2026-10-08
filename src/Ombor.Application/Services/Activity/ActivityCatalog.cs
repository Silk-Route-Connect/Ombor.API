using Ombor.Contracts.Enums;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using DomainPaymentType = Ombor.Domain.Enums.PaymentType;
using DomainTransactionType = Ombor.Domain.Enums.TransactionType;

namespace Ombor.Application.Services.Activity;

/// <summary>
/// What the Activity Log knows about each audited entity type, keyed by the CLR class name the audit row stores:
/// its public kind, how strongly it represents an operation, and which of its columns point at other records.
/// </summary>
internal static class ActivityCatalog
{
    public const string Transaction = nameof(TransactionRecord);
    public const string Payment = nameof(Domain.Entities.Payment);
    public const string Order = nameof(Domain.Entities.Order);

    private static readonly Dictionary<string, ActivityEntityKind> FixedKinds = new()
    {
        [nameof(StockAdjustment)] = ActivityEntityKind.Adjustment,
        [nameof(Domain.Entities.Transfer)] = ActivityEntityKind.Transfer,
        [nameof(Domain.Entities.OpeningStock)] = ActivityEntityKind.OpeningStock,
        [nameof(Domain.Entities.WalletTransfer)] = ActivityEntityKind.WalletTransfer,
        [Order] = ActivityEntityKind.Order,
        [nameof(Domain.Entities.Product)] = ActivityEntityKind.Product,
        [nameof(Domain.Entities.Category)] = ActivityEntityKind.Category,
        [nameof(Domain.Entities.Partner)] = ActivityEntityKind.Partner,
        [nameof(Domain.Entities.Wallet)] = ActivityEntityKind.Wallet,
        [nameof(Domain.Entities.Warehouse)] = ActivityEntityKind.Warehouse,
        [nameof(Domain.Entities.Employee)] = ActivityEntityKind.Employee,
        [nameof(Domain.Entities.Template)] = ActivityEntityKind.Template,
        [nameof(Domain.Entities.Organization)] = ActivityEntityKind.Organization,
        [nameof(Domain.Entities.User)] = ActivityEntityKind.User,
        [nameof(Domain.Entities.TransactionLine)] = ActivityEntityKind.TransactionLine,
        [nameof(Domain.Entities.OrderLine)] = ActivityEntityKind.OrderLine,
        [nameof(Domain.Entities.OrderStatusEvent)] = ActivityEntityKind.OrderStatusEvent,
        [nameof(Domain.Entities.TemplateItem)] = ActivityEntityKind.TemplateItem,
        [nameof(Domain.Entities.TransferLine)] = ActivityEntityKind.TransferLine,
        [nameof(Domain.Entities.PaymentComponent)] = ActivityEntityKind.PaymentComponent,
        [nameof(Domain.Entities.PaymentAllocation)] = ActivityEntityKind.PaymentAllocation,
        [nameof(WarehouseItem)] = ActivityEntityKind.Stock,
    };

    // A part's column that points at the record it belongs to: implied by the operation, so not shown as a field.
    private static readonly Dictionary<string, string> ParentKeys = new()
    {
        [nameof(Domain.Entities.TransactionLine)] = nameof(Domain.Entities.TransactionLine.TransactionId),
        [nameof(Domain.Entities.OrderLine)] = nameof(Domain.Entities.OrderLine.OrderId),
        [nameof(Domain.Entities.OrderStatusEvent)] = nameof(Domain.Entities.OrderStatusEvent.OrderId),
        [nameof(Domain.Entities.TemplateItem)] = nameof(Domain.Entities.TemplateItem.TemplateId),
        [nameof(Domain.Entities.TransferLine)] = nameof(Domain.Entities.TransferLine.TransferId),
        [nameof(Domain.Entities.PaymentComponent)] = nameof(Domain.Entities.PaymentComponent.PaymentId),
        [nameof(Domain.Entities.PaymentAllocation)] = nameof(Domain.Entities.PaymentAllocation.PaymentId),
    };

    /// <summary>Columns that reference another record, and the entity type they point at.</summary>
    public static readonly IReadOnlyDictionary<string, string> References = new Dictionary<string, string>
    {
        ["PartnerId"] = nameof(Domain.Entities.Partner),
        ["CustomerId"] = nameof(Domain.Entities.Partner),
        ["ProductId"] = nameof(Domain.Entities.Product),
        ["WarehouseId"] = nameof(Domain.Entities.Warehouse),
        ["FromWarehouseId"] = nameof(Domain.Entities.Warehouse),
        ["ToWarehouseId"] = nameof(Domain.Entities.Warehouse),
        ["WalletId"] = nameof(Domain.Entities.Wallet),
        ["FromWalletId"] = nameof(Domain.Entities.Wallet),
        ["ToWalletId"] = nameof(Domain.Entities.Wallet),
        ["CategoryId"] = nameof(Domain.Entities.Category),
        ["EmployeeId"] = nameof(Domain.Entities.Employee),
        ["TransactionId"] = Transaction,
        ["OriginalTransactionId"] = Transaction,
        ["SaleId"] = Transaction,
    };

    /// <summary>
    /// Rows that are part of another record or a consequence of it (document lines, payment parts, stock rows). They
    /// never stand for an operation on their own while their record is in it, and an action filter ignores them.
    /// </summary>
    public static readonly string[] PartTypes =
    [
        .. ParentKeys.Keys,
        nameof(WarehouseItem),
    ];

    public static bool IsPart(string entityType) => PartTypes.Contains(entityType);

    public static string? ParentKeyOf(string entityType) => ParentKeys.GetValueOrDefault(entityType);

    /// <summary>The stored entity type behind a public kind.</summary>
    public static string EntityTypeOf(ActivityEntityKind kind) => kind switch
    {
        ActivityEntityKind.Sale or ActivityEntityKind.Supply or ActivityEntityKind.SaleRefund or ActivityEntityKind.SupplyRefund => Transaction,
        ActivityEntityKind.Payment or ActivityEntityKind.Payroll => Payment,
        _ => FixedKinds.First(pair => pair.Value == kind).Key,
    };

    /// <summary>The transaction type a kind stands for; null for every other kind.</summary>
    public static DomainTransactionType? TransactionTypeOf(ActivityEntityKind kind) => kind switch
    {
        ActivityEntityKind.Sale => DomainTransactionType.Sale,
        ActivityEntityKind.Supply => DomainTransactionType.Supply,
        ActivityEntityKind.SaleRefund => DomainTransactionType.SaleRefund,
        ActivityEntityKind.SupplyRefund => DomainTransactionType.SupplyRefund,
        _ => null,
    };

    /// <summary>
    /// The public kind of a stored row. Transactions need their type and payments theirs (payroll or not); a document
    /// that no longer exists falls back to the sale / payment kind.
    /// </summary>
    public static ActivityEntityKind KindOf(string entityType, DomainTransactionType? transactionType, DomainPaymentType? paymentType) =>
        entityType switch
        {
            Transaction => transactionType switch
            {
                DomainTransactionType.Supply => ActivityEntityKind.Supply,
                DomainTransactionType.SaleRefund => ActivityEntityKind.SaleRefund,
                DomainTransactionType.SupplyRefund => ActivityEntityKind.SupplyRefund,
                _ => ActivityEntityKind.Sale,
            },
            Payment => paymentType == DomainPaymentType.Payroll ? ActivityEntityKind.Payroll : ActivityEntityKind.Payment,
            _ => FixedKinds[entityType],
        };

    public static bool IsKnown(string entityType) =>
        entityType is Transaction or Payment || FixedKinds.ContainsKey(entityType);

    /// <summary>
    /// How strongly a row stands for its operation (lower wins): an order transition or edit names the operation
    /// even when delivery creates a sale; a created document beats the debts it settled; master data comes next; a
    /// document's settlement update, its parts and stock rows last.
    /// </summary>
    public static int RankOf(string entityType, AuditAction action) => entityType switch
    {
        Order => 0,
        Transaction when action == AuditAction.Created => 1,
        Payment when action == AuditAction.Created => 2,
        nameof(StockAdjustment) or nameof(Domain.Entities.Transfer) or nameof(Domain.Entities.OpeningStock)
            or nameof(Domain.Entities.WalletTransfer) => 3,
        nameof(Domain.Entities.Organization) => 4,
        nameof(Domain.Entities.User) => 5,
        Transaction or Payment => 7,
        nameof(WarehouseItem) => 9,
        _ when ParentKeys.ContainsKey(entityType) => 8,
        _ => 6,
    };
}
