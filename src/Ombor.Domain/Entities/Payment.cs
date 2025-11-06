using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class Payment : EntityBase
{
    public string? Notes { get; set; }
    public PaymentType Type { get; set; }
    public PaymentDirection Direction { get; set; }
    public DateTimeOffset DateUtc { get; set; }

    public int? PartnerId { get; set; }
    public virtual Partner? Partner { get; set; }

    public int? EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    public virtual ICollection<PaymentComponent> Components { get; set; } = [];
    public virtual ICollection<PaymentAllocation> Allocations { get; set; } = [];
    public virtual ICollection<PaymentAttachment> Attachments { get; set; } = [];
}
