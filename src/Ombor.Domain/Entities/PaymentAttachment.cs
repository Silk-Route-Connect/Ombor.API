using Ombor.Domain.Common;

namespace Ombor.Domain.Entities;

public class PaymentAttachment : EntityBase
{
    public required string FileId { get; set; }
    public required string FileName { get; set; }

    public int PaymentId { get; set; }
    public virtual required Payment Payment { get; set; }
}
