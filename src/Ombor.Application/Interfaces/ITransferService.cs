using Ombor.Contracts.Requests.Transfer;
using Ombor.Contracts.Responses.Transfer;

namespace Ombor.Application.Interfaces;

/// <summary>
/// Operations for inter-warehouse stock transfers.
/// </summary>
public interface ITransferService
{
    /// <summary>Lists transfers, optionally filtered to a single warehouse.</summary>
    Task<TransferDto[]> GetAsync(GetTransfersRequest request);

    /// <summary>Retrieves a single transfer by its identifier.</summary>
    Task<TransferDto> GetByIdAsync(GetTransferByIdRequest request);

    /// <summary>
    /// Moves stock between two warehouses as one atomic operation: decrements the source
    /// and increments the destination, carrying the weighted-average cost with the goods.
    /// </summary>
    Task<TransferDto> CreateAsync(CreateTransferRequest request);
}
