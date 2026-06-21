using Ombor.Contracts.Responses.Transfer;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class TransferMappings
{
    public static TransferDto ToDto(this Transfer transfer) =>
        new(
            transfer.Id,
            transfer.FromWarehouseId,
            transfer.FromWarehouse.Name,
            transfer.ToWarehouseId,
            transfer.ToWarehouse.Name,
            transfer.DateUtc,
            transfer.Status.ToString(),
            transfer.Notes,
            [.. transfer.Lines.Select(l => new TransferLineDto(l.Id, l.ProductId, l.Product.Name, (int)l.Quantity))]);
}
