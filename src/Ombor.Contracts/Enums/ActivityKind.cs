using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// What an Activity Log operation did, as one value per record kind × action — the sentence a client renders
/// («создал продажу №42»). Documents are immutable, so they are only created (or, for a sale/supply/refund, updated
/// by a payment settling it). <see cref="Other"/> is the fallback for a combination no write path produces.
/// </summary>
[JsonConverter(typeof(Serialization.ValidatingStringEnumConverter))]
public enum ActivityKind
{
    Other = 0,

    SaleCreated = 1,
    SaleUpdated = 2,
    SupplyCreated = 3,
    SupplyUpdated = 4,
    SaleRefundCreated = 5,
    SaleRefundUpdated = 6,
    SupplyRefundCreated = 7,
    SupplyRefundUpdated = 8,

    PaymentCreated = 10,
    PayrollPaid = 11,
    AdjustmentCreated = 12,
    TransferCreated = 13,
    OpeningStockCreated = 14,
    WalletTransferCreated = 15,

    /// <summary>Stock changed with no document in the same operation (rows recorded before operations existed).</summary>
    StockChanged = 16,

    OrderCreated = 20,
    OrderUpdated = 21,

    /// <summary>An order moved to another status; delivery also creates the sale, listed among the changes.</summary>
    OrderStatusChanged = 22,

    ProductCreated = 30,
    ProductUpdated = 31,
    ProductArchived = 32,
    ProductRestored = 33,
    ProductDeleted = 34,

    CategoryCreated = 40,
    CategoryUpdated = 41,
    CategoryDeleted = 42,

    PartnerCreated = 50,
    PartnerUpdated = 51,
    PartnerArchived = 52,
    PartnerRestored = 53,
    PartnerDeleted = 54,

    WalletCreated = 60,
    WalletUpdated = 61,
    WalletArchived = 62,
    WalletRestored = 63,
    WalletDeleted = 64,

    WarehouseCreated = 70,
    WarehouseUpdated = 71,
    WarehouseArchived = 72,
    WarehouseRestored = 73,
    WarehouseDeleted = 74,

    EmployeeCreated = 80,
    EmployeeUpdated = 81,
    EmployeeDeleted = 82,

    TemplateCreated = 90,
    TemplateUpdated = 91,
    TemplateDeleted = 92,

    OrganizationCreated = 100,
    OrganizationUpdated = 101,

    UserCreated = 110,
    UserUpdated = 111,
}
