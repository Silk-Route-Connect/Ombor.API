using Ombor.Contracts.Enums;

namespace Ombor.Contracts.Abstractions;

public interface IOrderStateUpdateRequest
{
    public int OrderId { get; }
    public OrderStatus TargetStatus { get; }
}
