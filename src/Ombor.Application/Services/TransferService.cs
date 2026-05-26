using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Transfer;
using Ombor.Contracts.Responses.Transfer;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class TransferService(
    IApplicationDbContext context,
    IRequestValidator validator) : ITransferService
{
    public async Task<TransferDto[]> GetAsync(GetTransfersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = GetQuery();

        if (request.InventoryId.HasValue)
        {
            var inventoryId = request.InventoryId.Value;
            query = query.Where(t => t.FromInventoryId == inventoryId || t.ToInventoryId == inventoryId);
        }

        var transfers = await query
            .OrderByDescending(t => t.DateUtc)
            .ToArrayAsync();

        return [.. transfers.Select(x => x.ToDto())];
    }

    public async Task<TransferDto> GetByIdAsync(GetTransferByIdRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = await GetQuery()
            .FirstOrDefaultAsync(t => t.Id == request.Id)
            ?? throw new EntityNotFoundException<Transfer>(request.Id);

        return transfer.ToDto();
    }

    public async Task<TransferDto> CreateAsync(CreateTransferRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        if (request.FromInventoryId == request.ToInventoryId)
        {
            throw new ValidationException("Source and destination warehouses must be different.");
        }

        await EnsureInventoryExistsAsync(request.FromInventoryId, "Source");
        await EnsureInventoryExistsAsync(request.ToInventoryId, "Destination");

        var lines = request.Lines
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToArray();
        var productIds = lines.Select(l => l.ProductId).ToArray();

        var items = await context.InventoryItems
            .Where(i => (i.InventoryId == request.FromInventoryId || i.InventoryId == request.ToInventoryId)
                && productIds.Contains(i.ProductId))
            .ToListAsync();

        var transfer = new Transfer
        {
            FromInventoryId = request.FromInventoryId,
            ToInventoryId = request.ToInventoryId,
            DateUtc = DateTimeOffset.UtcNow,
            Status = TransferStatus.Completed,
            Notes = request.Notes,
            FromInventory = null!,
            ToInventory = null!,
        };

        foreach (var line in lines)
        {
            var source = items.FirstOrDefault(
                i => i.InventoryId == request.FromInventoryId && i.ProductId == line.ProductId);

            if (source is null || source.Quantity < line.Quantity)
            {
                throw new ValidationException(
                    $"Insufficient stock for product {line.ProductId} in the source warehouse.");
            }

            source.Quantity -= line.Quantity;

            var destination = items.FirstOrDefault(
                i => i.InventoryId == request.ToInventoryId && i.ProductId == line.ProductId);

            if (destination is null)
            {
                destination = new InventoryItem
                {
                    InventoryId = request.ToInventoryId,
                    ProductId = line.ProductId,
                    Quantity = 0,
                    AverageCost = 0m,
                    Inventory = null!,
                    Product = null!,
                };
                context.InventoryItems.Add(destination);
                items.Add(destination);
            }

            // Stock moves at its carrying cost — weighted-average it into the destination.
            var newQuantity = destination.Quantity + line.Quantity;
            destination.AverageCost = newQuantity == 0
                ? 0m
                : ((destination.Quantity * destination.AverageCost) + (line.Quantity * source.AverageCost)) / newQuantity;
            destination.Quantity = newQuantity;

            transfer.Lines.Add(new TransferLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                Product = null!,
                Transfer = null!,
            });
        }

        context.Transfers.Add(transfer);
        await context.SaveChangesAsync();

        return await GetByIdAsync(new GetTransferByIdRequest(transfer.Id));
    }

    private IQueryable<Transfer> GetQuery() =>
        context.Transfers
            .IgnoreAutoIncludes()
            .Include(t => t.FromInventory)
            .Include(t => t.ToInventory)
            .Include(t => t.Lines)
            .ThenInclude(l => l.Product)
            .AsNoTracking();

    private async Task EnsureInventoryExistsAsync(int inventoryId, string role)
    {
        if (!await context.Inventories.AnyAsync(i => i.Id == inventoryId))
        {
            throw new ValidationException($"{role} warehouse {inventoryId} does not exist.");
        }
    }
}
