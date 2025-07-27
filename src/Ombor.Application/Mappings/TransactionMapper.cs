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
        return new TransactionRecord
        {
            PartnerId = request.PartnerId,
            DateUtc = DateTimeOffset.UtcNow,
            Type = request.Type.ToDomainType(),
            Partner = null!,
            Lines = request.Lines.Select(x => new TransactionLine
            {
                ProductId = x.ProductId,
                UnitPrice = x.UnitPrice,
                Discount = x.Discount,
                Quantity = x.Quantity,
                Product = null!,
                Transaction = null!
            }).ToArray(),
            TotalDue = request.Lines.Sum(CalculateLineTotal),
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
                x => new TransactionLineDto(x.Id, x.ProductId, x.Product.Name, x.TransactionId, x.UnitPrice, x.Discount, x.Quantity, x.Total)));
    }

    private static decimal CalculateLineTotal(CreateTransactionLine l)
        => l.UnitPrice * l.Quantity * (1 - (l.Discount / 100m));
}
