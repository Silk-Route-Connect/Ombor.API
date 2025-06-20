using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Transactions;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class TransactionService(
    IApplicationDbContext context,
    ITransactionMappings mapper,
    IRequestValidator validator,
    ICurrencyCalculator currencyCalculator,
    IDateTimeProvider dateTimeProvider) : ITransactionService
{
    public async Task<TransactionDto[]> GetAsync(GetTransactionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery(request);
        var transactions = await query
            .OrderByDescending(x => x.DateUtc)
            .ToArrayAsync();

        return [.. transactions.Select(x => mapper.ToDto(x, []))];
    }

    public async Task<TransactionDto> GetByIdAsync(GetTransactionByIdRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var transaction = await GetOrThrowAsync(request.Id);

        return mapper.ToDto(transaction, []);
    }

    public async Task<CreateTransactionResponse> CreateAsync(CreateTransactionRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var entity = mapper.ToEntity(request);
        await using var dbTransaction = await context.Database.BeginTransactionAsync();

        var payment = CreatePayment(request, entity);

        context.Payments.Add(payment);
        context.Transactions.Add(entity);
        await context.SaveChangesAsync();

        return mapper.ToCreateResponse(entity);
    }

    public Task<UpdateTransactionResponse> UpdateAsync(UpdateTransactionRequest request)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(DeleteTransactionRequest request)
    {
        throw new NotImplementedException();
    }

    private IQueryable<TransactionRecord> GetQuery(GetTransactionsRequest request)
    {
        var query = context.Transactions
            .Include(x => x.Partner)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Notes != null && x.Notes.Contains(request.SearchTerm));
        }

        if (request.Type.HasValue)
        {
            var domainType = Enum.Parse<Domain.Enums.TransactionType>(request.Type.Value.ToString());
            query = query.Where(x => x.Type == domainType);
        }

        if (request.Status.HasValue)
        {
            var domainStatus = Enum.Parse<Domain.Enums.TransactionStatus>(request.Status.Value.ToString());
            query = query.Where(x => x.Status == domainStatus);
        }

        if (request.PartnerId.HasValue)
        {
            query = query.Where(x => x.PartnerId == request.PartnerId.Value);
        }

        return query;
    }

    private async Task<TransactionRecord> GetOrThrowAsync(int transactionId) =>
        await context.Transactions
        .Include(x => x.Partner)
        .FirstOrDefaultAsync(x => x.Id == transactionId)
        ?? throw new EntityNotFoundException<TransactionRecord>(transactionId);

    private Payment CreatePayment(CreateTransactionRequest request, TransactionRecord transaction)
    {
        var payment = new Payment
        {
            Notes = request.Notes,
            ExternalReference = null,
            Amount = request.TotalPaid,
            AmountLocal = currencyCalculator.CalculateLocalAmount(request.TotalPaid, request.ExchangeRate),
            ExchangeRate = request.ExchangeRate,
            DateUtc = dateTimeProvider.UtcNow,
            Type = PaymentType.Transaction,
            Method = request.Method.ParseToDomain(),
            Currency = request.Currency.ParseToDomain(),
            Direction = request.Type.GetPaymentDirection(),

        };
        var allocation = new PaymentAllocation
        {
            AppliedAmount = request.TotalPaid,
            Type = request.Type.GetPaymentAllocationType(),
            Transaction = transaction,
            Payment = payment,
        };
        payment.Allocations.Add(allocation);

        return payment;
    }
}
