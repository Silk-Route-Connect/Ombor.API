using System.Text.Json.Serialization;

namespace Ombor.Contracts.Enums;

/// <summary>
/// The kind of record an Activity Log entry is about. Transactions are split by type and payments into payments and
/// payroll, so a client can name and link the record without a second request. The last group are parts of another
/// record; they appear only inside an operation's changes.
/// </summary>
[JsonConverter(typeof(Serialization.ValidatingStringEnumConverter))]
public enum ActivityEntityKind
{
    /// <summary>A sale (<c>/api/transactions/{id}</c>); label is its document number.</summary>
    Sale = 1,

    /// <summary>A supply (<c>/api/transactions/{id}</c>); label is its document number.</summary>
    Supply = 2,

    /// <summary>A sale refund (<c>/api/transactions/{id}</c>); label is its document number.</summary>
    SaleRefund = 3,

    /// <summary>A supply refund (<c>/api/transactions/{id}</c>); label is its document number.</summary>
    SupplyRefund = 4,

    /// <summary>A payment other than payroll (<c>/api/payments/{id}</c>); label is its document number.</summary>
    Payment = 5,

    /// <summary>A payroll payment (<c>/api/payments/{id}</c>); label is its document number.</summary>
    Payroll = 6,

    /// <summary>A stock adjustment; label is its id (adjustments have no document number).</summary>
    Adjustment = 7,

    /// <summary>A transfer between warehouses (<c>/api/transfers/{id}</c>); label is its id.</summary>
    Transfer = 8,

    /// <summary>An opening-stock record; label is the product's name.</summary>
    OpeningStock = 9,

    /// <summary>A transfer between wallets; label is its id.</summary>
    WalletTransfer = 10,

    /// <summary>An order (<c>/api/orders/{id}</c>); label is its order number.</summary>
    Order = 11,

    /// <summary>A product; label is its name.</summary>
    Product = 12,

    /// <summary>A category; label is its name.</summary>
    Category = 13,

    /// <summary>A partner; label is its name.</summary>
    Partner = 14,

    /// <summary>A wallet; label is its name.</summary>
    Wallet = 15,

    /// <summary>A warehouse; label is its name.</summary>
    Warehouse = 16,

    /// <summary>An employee; label is the full name.</summary>
    Employee = 17,

    /// <summary>A template; label is its name.</summary>
    Template = 18,

    /// <summary>The organization's business profile; label is its name.</summary>
    Organization = 19,

    /// <summary>A user of the organization; label is the user's name.</summary>
    User = 20,

    /// <summary>A line of a sale, supply or refund; label is the product's name.</summary>
    TransactionLine = 21,

    /// <summary>A line of an order; label is the product's name.</summary>
    OrderLine = 22,

    /// <summary>An order status transition; no label.</summary>
    OrderStatusEvent = 23,

    /// <summary>An item of a template; label is the product's name.</summary>
    TemplateItem = 24,

    /// <summary>A line of a warehouse transfer; label is the product's name.</summary>
    TransferLine = 25,

    /// <summary>Where a payment's money came from; label is the wallet's name (none for an advance).</summary>
    PaymentComponent = 26,

    /// <summary>Where a payment's money went; label is the settled document's number (none for an advance or change).</summary>
    PaymentAllocation = 27,

    /// <summary>A product's stock in one warehouse (quantity and average cost); label is the product's name.</summary>
    Stock = 28,
}
