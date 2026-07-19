using Ombor.Application.Extensions;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal interface ITransactionMapper
{
    TransactionRecord ToEntity(CreateTransactionRequest request, IReadOnlyDictionary<int, int> packageSizes);
    TransactionDto ToDto(TransactionRecord transaction);
}

internal sealed class TransactionMapper : ITransactionMapper
{
    public TransactionRecord ToEntity(CreateTransactionRequest request, IReadOnlyDictionary<int, int> packageSizes)
    {
        var lines = request.Lines.Select(x =>
        {
            // A package-entry line resolves to base units server-side (rule 21); the package size is snapshotted.
            var (quantity, packageSize) = PackageEntry.Resolve(x.ProductId, x.Quantity, x.PackageQuantity, packageSizes);

            return new TransactionLine
            {
                ProductId = x.ProductId,
                UnitPrice = x.UnitPrice,
                Discount = x.Discount,
                DiscountType = x.DiscountType.ToDomainDiscountType(),
                Quantity = quantity,
                PackageSize = packageSize,
                Product = null!,
                Transaction = null!
            };
        }).ToArray();

        return new TransactionRecord
        {
            PartnerId = request.PartnerId,
            // Non-null is guaranteed upstream: the service rejects a missing warehouse before mapping.
            WarehouseId = request.WarehouseId!.Value,
            OriginalTransactionId = request.OriginalTransactionId,
            RefundReason = request.RefundReason,
            Notes = request.Notes,
            DateUtc = DateTimeOffset.UtcNow,
            DueDate = request.DueDate,
            Type = request.Type.ToDomainType(),
            Partner = null!,
            Lines = lines,
            // Line totals already apply rule 37 (percentage vs fixed, clamped); sum them for the due.
            TotalDue = lines.Sum(l => l.Total),
            TotalPaid = 0,
            Status = Domain.Enums.TransactionStatus.Open,
        };
    }

    public TransactionDto ToDto(TransactionRecord transaction)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new TransactionDto(
            transaction.Id,
            transaction.Number.ToString(),
            transaction.PartnerId,
            transaction.Partner.Name,
            transaction.DateUtc,
            transaction.Type.ToString(),
            transaction.Status.ToEffectiveStatusName(transaction.DueDate, today),
            transaction.TotalDue,
            transaction.TotalPaid,
            transaction.Lines.Select(
                x => new TransactionLineDto(x.Id, x.ProductId, x.Product.Name, x.TransactionId, x.UnitPrice, x.Discount, x.DiscountType.ToString(), x.Quantity, x.Total, x.PackageSize)),
            transaction.OriginalTransactionId,
            transaction.OriginalTransaction?.Number.ToString(),
            transaction.RefundReason);
    }
}
