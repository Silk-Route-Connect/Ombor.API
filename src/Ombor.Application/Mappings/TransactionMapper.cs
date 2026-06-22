using Ombor.Application.Extensions;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;

namespace Ombor.Application.Mappings;

internal interface ITransactionMapper
{
    TransactionRecord ToEntity(CreateTransactionRequest request);
    TransactionDto ToDto(TransactionRecord transaction);
}

internal sealed class TransactionMapper : ITransactionMapper
{
    public TransactionRecord ToEntity(CreateTransactionRequest request)
    {
        var lines = request.Lines.Select(x => new TransactionLine
        {
            ProductId = x.ProductId,
            UnitPrice = x.UnitPrice,
            Discount = x.Discount,
            DiscountType = x.DiscountType.ToDomainDiscountType(),
            Quantity = x.Quantity,
            Product = null!,
            Transaction = null!
        }).ToArray();

        return new TransactionRecord
        {
            PartnerId = request.PartnerId,
            WarehouseId = request.WarehouseId,
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
        return new TransactionDto(
            transaction.Id,
            transaction.PartnerId,
            transaction.Partner.Name,
            transaction.DateUtc,
            transaction.Type.ToString(),
            transaction.Status.ToString(),
            transaction.TotalDue,
            transaction.TotalPaid,
            transaction.Lines.Select(
                x => new TransactionLineDto(x.Id, x.ProductId, x.Product.Name, x.TransactionId, x.UnitPrice, x.Discount, x.DiscountType.ToString(), x.Quantity, x.Total)),
            transaction.OriginalTransactionId,
            transaction.RefundReason);
    }
}
