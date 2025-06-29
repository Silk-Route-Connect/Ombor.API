using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Payments;
using Ombor.Contracts.Requests.Transactions;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class TransactionService(
    IApplicationDbContext context,
    ITransactionMapper mapper,
    IRequestValidator validator,
    ITransactionPaymentService transactionPaymentService) : ITransactionService
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

        var transaction = mapper.ToEntity(request);
        transaction.Partner = await GetOrThrowPartnerAsync(request.PartnerId);

        await using var dbTransaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            if (request.TotalPaid > 0)
            {
                var paymentRequest = new CreateTransactionPaymentRequest(
                    TransactionId: transaction.Id,
                    Notes: request.Notes,
                    Amount: request.TotalPaid,
                    ExchangeRate: request.ExchangeRate,
                    Currency: request.Currency,
                    Method: request.PaymentMethod,
                    Attachments: request.Attachments);

                await transactionPaymentService.CreatePaymentAsync(paymentRequest);
            }

            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }

        var created = await GetOrThrowAsync(transaction.Id);
        return mapper.ToCreateResponse(created);
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
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
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
        .Include(x => x.Lines)
        .ThenInclude(x => x.Product)
        .FirstOrDefaultAsync(x => x.Id == transactionId)
        ?? throw new EntityNotFoundException<TransactionRecord>(transactionId);

    private async Task<Partner> GetOrThrowPartnerAsync(int partnerId)
        => await context.Partners.FirstOrDefaultAsync(x => x.Id == partnerId)
        ?? throw new EntityNotFoundException<Partner>(partnerId);
}
