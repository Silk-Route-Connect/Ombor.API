using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Extensions;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class TransactionService(
    IApplicationDbContext context,
    ITransactionMapper mapper,
    IPaymentService paymentService,
    IRequestValidator validator) : ITransactionService
{
    public Task<TransactionDto[]> GetAsync(GetTransactionsRequest request)
    {
        var query = GetQuery(request);

        return query
            .OrderByDescending(x => x.DateUtc)
            .Select(x => new TransactionDto(
                x.Id,
                x.PartnerId,
                x.Partner.Name,
                x.DateUtc,
                x.Type.ToString(),
                x.Status.ToString(),
                x.TotalDue,
                x.TotalPaid,
                x.Lines.Select(l => new TransactionLineDto(l.Id, l.ProductId, l.Product.Name, l.TransactionId, l.UnitPrice, l.Discount, l.Quantity, l.Total))))
            .ToArrayAsync();
    }

    public async Task<TransactionDto> GetByIdAsync(GetTransactionByIdRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transaction = await context.Transactions
            .Include(x => x.Partner)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id)
            ?? throw new EntityNotFoundException<TransactionRecord>($"Transaction with ID {request.Id} not found.");

        return mapper.ToDto(transaction);
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request)
    {
        await ValidateOrThrowAsync(request);

        var transactionEntity = mapper.ToEntity(request);
        var partner = await context.Partners.FindAsync(request.PartnerId)
                ?? throw new InvalidOperationException($"Partner {request.PartnerId} not found");

        await using var databaseTransaction = await context.Database.BeginTransactionAsync();
        try
        {
            await UpdateProducts(request);
            context.Transactions.Add(transactionEntity);
            await context.SaveChangesAsync();

            var paymentRequest = request.ToPaymentRequest(transactionEntity.Id);
            var payment = await paymentService.CreateAsync(paymentRequest);

            if (payment is not null)
            {
                var totalPaid = payment.Allocations
                    .Where(a => a.TransactionId == transactionEntity.Id)
                    .Sum(a => a.Amount);

                transactionEntity.AddPayment(totalPaid);
            }

            await context.SaveChangesAsync();
            await databaseTransaction.CommitAsync();

            transactionEntity.Partner = partner;
            transactionEntity.Lines = await context.TransactionLines
                .Include(x => x.Product)
                .Where(x => x.TransactionId == transactionEntity.Id)
                .ToArrayAsync();

            return mapper.ToDto(transactionEntity);
        }
        catch
        {
            await databaseTransaction.RollbackAsync();
            throw;
        }
    }

    private async Task ValidateOrThrowAsync(CreateTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request);

        var partner = await context.Partners
            .FirstOrDefaultAsync(x => x.Id == request.PartnerId)
            ?? throw new InvalidOperationException("Partner does not exist");
        var partnerBalance = await context.PartnerBalances
            .FirstAsync(x => x.PartnerId == request.PartnerId);
        var totalDebt = request.Type == Contracts.Enums.TransactionType.Sale
            ? Math.Abs(partnerBalance.PayableDebt)
            : Math.Abs(partnerBalance.ReceivableDebt);

        var totalDue = request.Lines.Sum(CalculateLineTotal);
        var totalPaid = request.Payments.Sum(x => x.Amount * x.ExchangeRate);
        var totalPaidDebt = request.DebtPayments?.Sum(x => x.Amount) ?? 0;
        var totalPaidAdvance = totalPaid - totalDue - totalPaidDebt; // if negative, no advance payment

        if (totalPaid < totalDue && totalPaidDebt > 0)
        {
            throw new ValidationException("Debt payment is not allowed without fully covering current debt.");
        }

        if (totalDebt < totalPaidDebt)
        {
            throw new ValidationException("Debt payment cannot be greater than partner's total debt amount.");
        }

        if (totalDebt > totalPaidDebt && totalPaidAdvance > 0)
        {
            throw new ValidationException("Cannot make advance payment without closing existing debts.");
        }

        if (!partner.CanHandleTransaction(request.Type))
        {
            throw new ValidationException($"Partner of type: {partner.Type} cannot have transactions of type: {request.Type}");
        }

        var creditRequired = request.Payments
                .Where(p => p.Method == Contracts.Enums.PaymentMethod.AccountBalance)
                .Sum(p => p.Amount * p.ExchangeRate);

        if (creditRequired <= 0)
        {
            return;
        }

        if (request.Type.GetPaymentDirection() == PaymentDirection.Income && partnerBalance.PartnerAdvance < creditRequired)
        {
            throw new ValidationException("Insufficient partner advance balance.");
        }

        if (request.Type.GetPaymentDirection() == PaymentDirection.Expense && partnerBalance.CompanyAdvance < creditRequired)
        {
            throw new ValidationException("Insufficient company advance balance.");
        }
    }

    private static decimal CalculateLineTotal(CreateTransactionLine l)
        => l.UnitPrice * l.Quantity * (1 - (l.Discount / 100m));

    // TODO: Add logic for refunds
    private async Task UpdateProducts(CreateTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lineProducts = request.Lines
            .ToDictionary(x => x.ProductId);
        var productIds = lineProducts.Keys.ToArray();
        var productsToUpdate = await context.Products
            .Where(x => productIds.Contains(x.Id))
            .ToArrayAsync();

        if (request.Type == Contracts.Enums.TransactionType.Supply)
        {
            foreach (var productToUpdate in productsToUpdate)
            {
                var lineProduct = lineProducts[productToUpdate.Id];
                productToUpdate.QuantityInStock += lineProduct.Quantity;
            }

            return;
        }

        foreach (var productToUpdate in productsToUpdate)
        {
            var lineProduct = lineProducts[productToUpdate.Id];
            if (productToUpdate.QuantityInStock < lineProduct.Quantity)
            {
                throw new ValidationException($"Product stock is not enough for sale."); // TODO: replace with domain exception
            }

            productToUpdate.QuantityInStock -= lineProduct.Quantity;
        }
    }

    private IQueryable<TransactionRecord> GetQuery(GetTransactionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = context.Transactions
            .Include(x => x.Partner)
            .Include(x => x.Lines)
            .ThenInclude(x => x.Product)
            .IgnoreAutoIncludes()
            .AsNoTracking();

        if (request.PartnerId.HasValue)
        {
            query = query.Where(x => x.PartnerId == request.PartnerId.Value);
        }

        if (request.Status.HasValue)
        {
            var domainStatus = request.Status.Value.ToDomainStatus();
            query = query.Where(x => x.Status == domainStatus);
        }

        if (request.Types is { Count: > 0 })
        {
            var domainTypes = request.Types
                .Select(x => x.ToDomainType())
                .ToList();

            query = query.Where(x => domainTypes.Contains(x.Type));
        }

        return query;
    }
}
