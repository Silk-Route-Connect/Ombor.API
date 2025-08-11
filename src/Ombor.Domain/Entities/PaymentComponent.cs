using Ombor.Domain.Common;
using Ombor.Domain.Enums;

namespace Ombor.Domain.Entities;

public class PaymentComponent : EntityBase
{
    public required decimal Amount { get; set; }
    public required decimal ExchangeRate { get; set; }
    public required string Currency { get; set; }
    public required PaymentMethod Method { get; set; }

    public int PaymentId { get; set; }
    public virtual required Payment Payment { get; set; }
}
