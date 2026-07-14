using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class Payment : EntityBase, IOrganizationScoped, IAuditable
{
    public int OrganizationId { get; set; }

    /// <summary>Human-facing payment number, sequential per organization. Null only on synthetic seed/test rows.</summary>
    public int? Number { get; set; }

    public string? Notes { get; set; }
    public PaymentType Type { get; set; }
    public PaymentDirection Direction { get; set; }
    public DateTimeOffset DateUtc { get; set; }

    /// <summary>The primary wallet the payment moves through (the wallet side of its sources); null for advance-only flows.</summary>
    public int? WalletId { get; set; }
    public virtual Wallet? Wallet { get; set; }

    public int? PartnerId { get; set; }
    public virtual Partner? Partner { get; set; }

    public int? EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    /// <summary>Payroll period (e.g. «2026-06»). Set only on Payroll payments.</summary>
    public string? Period { get; set; }

    /// <summary>The employee's salary snapshotted when this payroll was paid. Set only on Payroll payments.</summary>
    public decimal? Salary { get; set; }

    public virtual ICollection<PaymentComponent> Components { get; set; } = [];
    public virtual ICollection<PaymentAllocation> Allocations { get; set; } = [];
    public virtual ICollection<PaymentAttachment> Attachments { get; set; } = [];
}
