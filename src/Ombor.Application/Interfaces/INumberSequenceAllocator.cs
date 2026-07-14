using Ombor.Domain.Enums;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Allocates the next sequential document number for the current organization's series.
/// </summary>
public interface INumberSequenceAllocator
{
    /// <summary>
    /// Atomically allocates and returns the next number for the given series in the current
    /// organization. Must be called inside the caller's ambient transaction so the allocation
    /// and the entity insert commit (or roll back) together — otherwise a rolled-back create
    /// would burn a number and leave a gap.
    /// </summary>
    Task<int> AllocateAsync(NumberSeriesType series, CancellationToken cancellationToken = default);
}
