using Ombor.Application.Extensions;
using Ombor.Contracts.Responses.Transfer;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal static class TransferMappings
{
    public static TransferDto ToDto(this Transfer transfer) =>
        new(
            transfer.Id,
            transfer.DateUtc,
            transfer.FromWarehouseId,
            transfer.FromWarehouse.Name,
            transfer.ToWarehouseId,
            transfer.ToWarehouse.Name,
            transfer.Notes,
            transfer.CreatedByUser.DisplayName(),
            [.. transfer.Lines.Select(l => new TransferLineDto(
                l.Id,
                l.ProductId,
                l.Product.Name,
                l.Product.SKU,
                l.Product.Measurement.ToString(),
                l.Quantity))]);
}
